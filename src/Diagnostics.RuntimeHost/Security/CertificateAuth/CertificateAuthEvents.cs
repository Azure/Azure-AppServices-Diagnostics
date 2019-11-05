using System;
using System.Threading.Tasks;
namespace Diagnostics.RuntimeHost.Security.CertificateAuth
{
    public class CertificateAuthEvents
    {
        /// <summary>
        /// A delegate assigned to this property will be invoked when the authentication fails.
        /// </summary>
        public Func<CertificateFailedContext, Task> OnAuthenticationFailed { get; set; } = context => Task.CompletedTask;

        /// <summary>
        /// A delegate assigned to this property will be invoked when a certificate has passed basic validation, but where custom validation may be needed.
        /// </summary>
        /// <remarks>
        /// You must provide a delegate for this property for authentication to occur.
        /// In your delegate you should construct an authentication principal from the user details,
        /// attach it to the context.Principal property and finally call context.Success();
        /// </remarks>
        public Func<CertificateValidatedContext, Task> OnCertificateValidated { get; set; } = context => Task.CompletedTask;

        /// <summary>
        /// Invoked when a certificate fails authentication.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual Task AuthenticationFailed(CertificateFailedContext context) => OnAuthenticationFailed(context);

        /// <summary>
        /// Invoked after a certificate has been validated
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual Task CertificateValidated(CertificateValidatedContext context) => OnCertificateValidated(context);
    }
}
