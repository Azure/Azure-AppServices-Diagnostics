using Microsoft.Extensions.Options;
namespace Diagnostics.RuntimeHost.Security.CertificateAuth
{
    public class CertificateAuthPostConfigureOptions : IPostConfigureOptions<CertificateAuthOptions>
    {
        public void PostConfigure(string name, CertificateAuthOptions options)
        {
           
        }
    }
}
