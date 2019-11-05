using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;

namespace Diagnostics.RuntimeHost.Security.CertificateAuth
{
    public class CertificateValidatedContext : ResultContext<CertificateAuthOptions>
    {
        /// <summary>
        /// Creates a new instance of <see cref="CertificateValidatedContext"/>.
        /// </summary>
        /// <param name="context">The HttpContext the validate context applies too.</param>
        /// <param name="scheme">The scheme used when the Certificate Authentication handler was registered.</param>
        /// <param name="options">The <see cref="CertificateAuthenticationOptions"/>.</param>
        public CertificateValidatedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            CertificateAuthOptions options)
            : base(context, scheme, options)
        {
        }

        /// <summary>
        /// The certificate to validate.
        /// </summary>
        public X509Certificate2 ClientCertificate { get; set; }
    }
}
