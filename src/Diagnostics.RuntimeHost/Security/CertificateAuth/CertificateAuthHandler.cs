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

        public List<string> AllowedCertSubjectNames { get; set; }

        public List<string> AllowedCertIssuers { get; set; }

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
                using ( var certificate = new X509Certificate2(clientCertBytes))
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

            } catch(Exception ex)
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
            return AllowedCertSubjectNames.Contains(certificate.GetNameInfo(X509NameType.SimpleName, false), StringComparer.OrdinalIgnoreCase)
                && AllowedCertIssuers.Contains(certificate.IssuerName.Name, StringComparer.OrdinalIgnoreCase)
                && certificate.Verify();
        }

        private ClaimsPrincipal CreatePrincipal(X509Certificate2 certificate)
        {
            var claims = new List<Claim>();

            var issuer = certificate.Issuer;
            claims.Add(new Claim("issuer", issuer, ClaimValueTypes.String, Options.ClaimsIssuer));

            var thumbprint = certificate.Thumbprint;
            claims.Add(new Claim(ClaimTypes.Thumbprint, thumbprint, ClaimValueTypes.Base64Binary, Options.ClaimsIssuer));

            var value = certificate.SubjectName.Name;
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.X500DistinguishedName, value, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            value = certificate.SerialNumber;
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.SerialNumber, value, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.DnsName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Dns, value, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.SimpleName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Name, value, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.EmailName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Email, value, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.UpnName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Upn, value, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.UrlName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Uri, value, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var identity = new ClaimsIdentity(claims, CertificateAuthDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }
    }
}
