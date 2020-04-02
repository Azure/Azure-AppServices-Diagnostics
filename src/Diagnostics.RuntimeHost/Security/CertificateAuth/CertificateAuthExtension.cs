using Microsoft.AspNetCore.Authentication;
using System;

namespace Diagnostics.RuntimeHost.Security.CertificateAuth
{
    /// <summary>
    /// This class provides the extension methods to add Certificate authentication capabilites to an HTTP Application pipeline.
    /// </summary>
    public static class CertificateAuthExtension
    {
        /// <summary>
        /// Adds certificate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddCertificateAuth(this AuthenticationBuilder builder) => builder.AddCertificateAuth(CertificateAuthDefaults.AuthenticationScheme);

        /// <summary>
        /// Adds certificate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme"></param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddCertificateAuth(this AuthenticationBuilder builder, string authenticationScheme)
            => builder.AddCertificateAuth(authenticationScheme, configureOptions: null);

        /// <summary>
        /// Adds certificate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme"></param>
        /// <param name="configureOptions"></param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddCertificateAuth(
            this AuthenticationBuilder builder,
            string authenticationScheme,
            Action<CertificateAuthOptions> configureOptions)
        {
            return builder.AddScheme<CertificateAuthOptions, CertificateAuthHandler>(authenticationScheme, configureOptions);
        }
    }
}
