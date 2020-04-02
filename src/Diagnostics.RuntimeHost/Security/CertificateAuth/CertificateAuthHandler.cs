using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Diagnostics.Logger;
using System.Text;

namespace Diagnostics.RuntimeHost.Security.CertificateAuth
{
    public class CertificateAuthHandler : AuthenticationHandler<CertificateAuthOptions>
    {
        public CertificateAuthHandler(IOptionsMonitor<CertificateAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        public IConfiguration Configuration { get; }

        public IEnumerable<string> AllowedCertSubjectNames { get; set; }

        public IEnumerable<string> AllowedCertIssuers { get; set; }

        /// <summary>
        /// The handler calls methods on the events which give the application control at certain points where processing is occurring.
        /// If it is not provided a default instance is supplied which does nothing when the methods are called.
        /// </summary>
        protected new CertificateAuthEvents Events
        {
            get { return (CertificateAuthEvents)base.Events; }
            set { base.Events = value; }
        }

        /// <summary>
        /// Creates a new instance of the events instance.
        /// </summary>
        /// <returns>A new instance of the events instance.</returns>
        protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new CertificateAuthEvents());

        /// <summary>
        /// Handles validation of incoming client certificate supplied in request header.
        /// If we want to use App Service way of validating against client certificate, we need to check for X-ARR-ClientCert.
        /// </summary>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var certHeader = Request.Headers["x-ms-diagcert"];
            if (string.IsNullOrWhiteSpace(certHeader))
            {
                return AuthenticateResult.NoResult();
            }
            try
            {
                byte[] clientCertBytes = Convert.FromBase64String(certHeader);
                using (var certificate = new X509Certificate2(clientCertBytes))
                {
                    var certSubject = certificate.Subject;
                    var certIssuer = certificate.Issuer;
                    AllowedCertIssuers = Options.AllowedIssuers;
                    AllowedCertSubjectNames = Options.AllowedSubjectNames;
                    if (IsValidCert(certificate))
                    {
                        var certificateValidatedContext = new CertificateValidatedContext(Context, Scheme, Options)
                        {
                            ClientCertificate = certificate,
                            Principal = CreatePrincipal(certificate)
                        };
                        certificateValidatedContext.Success();
                        return certificateValidatedContext.Result;
                    }
                    return AuthenticateResult.Fail("Request is not authorized");

                }

            }
            catch (Exception ex)
            {
                var authenticationFailedContext = new CertificateFailedContext(Context, Scheme, Options)
                {
                    Exception = ex
                };

                await Events.AuthenticationFailed(authenticationFailedContext);

                if (authenticationFailedContext.Result != null)
                {
                    return authenticationFailedContext.Result;
                }

                throw;
            }

        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            // Certificate authentication takes place at the connection level. We can't prompt once we're in
            // user code, so the best thing to do is Forbid, not Challenge.
            return HandleForbiddenAsync(properties);
        }

        private bool IsValidCert(X509Certificate2 certificate)
        {

            if (!AllowedCertSubjectNames.Contains(certificate.GetNameInfo(X509NameType.SimpleName, false), StringComparer.OrdinalIgnoreCase))
            {
                DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Certificate authentication failed. Supplied certificate with SubjectName: {certificate.GetNameInfo(X509NameType.SimpleName, false)} is not in the allowed list of certificate subject names.");
            }

            if (!AllowedCertIssuers.Contains(certificate.IssuerName.Name, StringComparer.OrdinalIgnoreCase))
            {
                DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Certificate authentication failed. Supplied certificate with Issuer: {certificate.IssuerName.Name} is not in the allowed list of certificate issuer names.");
            }

            bool certVerificationResult = certificate.Verify();

            if (!certVerificationResult)
            {
                try
                {
                    StringBuilder chainValidationFailure = new StringBuilder();
                    chainValidationFailure.AppendLine("Certificate validation failed with the following error.");

                    X509Chain chain = new X509Chain();
                    bool chainBuilt = chain.Build(certificate);
                    chainValidationFailure.AppendLine($"Chain building status: {chainBuilt.ToString()}");

                    if (chainBuilt == false)
                    {
                        foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                        {
                            chainValidationFailure.AppendLine($"Chain error: {chainStatus.Status.ToString()} {chainStatus.StatusInformation}");

                        }
                    }
                    else
                    {
                        chainValidationFailure.AppendLine($"Cert chain built sucessfully. We shouldn't be here. Needs further investigation.");
                    }
                    DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"{chainValidationFailure.ToString()}");
                }
                catch (Exception ex)
                {
                    DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Failure while building cert chain error reason. {ex.ToString()}");
                }
            }



            return AllowedCertSubjectNames.Contains(certificate.GetNameInfo(X509NameType.SimpleName, false), StringComparer.OrdinalIgnoreCase)
                && AllowedCertIssuers.Contains(certificate.IssuerName.Name, StringComparer.OrdinalIgnoreCase)
                && certVerificationResult;
        }

        private ClaimsPrincipal CreatePrincipal(X509Certificate2 certificate)
        {
            var claims = new List<Claim>();

            var issuer = certificate.Issuer;
            claims.Add(new Claim("issuer", issuer, ClaimValueTypes.String, Options.ClaimsIssuer));

            var thumbprint = certificate.Thumbprint;
            claims.Add(new Claim(ClaimTypes.Thumbprint, thumbprint, ClaimValueTypes.Base64Binary, Options.ClaimsIssuer));

            var certValues = new List<string>() {certificate.SubjectName.Name,
                                    certificate.SerialNumber,
                                    certificate.GetNameInfo(X509NameType.DnsName, false),
                                    certificate.GetNameInfo(X509NameType.SimpleName, false),
                                    certificate.GetNameInfo(X509NameType.EmailName, false),
                                    certificate.GetNameInfo(X509NameType.UpnName, false),
                                    certificate.GetNameInfo(X509NameType.UrlName, false)};

            var claimTypes = new List<string>() { ClaimTypes.X500DistinguishedName ,
                                    ClaimTypes.SerialNumber ,
                                    ClaimTypes.Dns ,
                                    ClaimTypes.Name,
                                    ClaimTypes.Email,
                                    ClaimTypes.Upn,
                                    ClaimTypes.Uri };

            var certDetailsMapping = certValues.Zip(claimTypes, (key, val) => new KeyValuePair<string, string>(key, val));

            foreach (var certPair in certDetailsMapping)
            {
                if (!string.IsNullOrWhiteSpace(certPair.Key))
                {
                    claims.Add(new Claim(certPair.Value, certPair.Key, ClaimValueTypes.String, Options.ClaimsIssuer));
                }
            }

            var identity = new ClaimsIdentity(claims, CertificateAuthDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }
    }
}