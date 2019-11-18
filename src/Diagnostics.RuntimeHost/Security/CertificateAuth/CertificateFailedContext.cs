using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;

namespace Diagnostics.RuntimeHost.Security.CertificateAuth
{
    public class CertificateFailedContext : ResultContext<CertificateAuthOptions>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scheme"></param>
        /// <param name="options"></param>
        public CertificateFailedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            CertificateAuthOptions options)
            : base(context, scheme, options)
        {
        }

        /// <summary>
        /// The exception.
        /// </summary>
        public Exception Exception { get; set; }
    }
}
