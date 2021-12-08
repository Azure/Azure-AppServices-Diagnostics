using System;
using Microsoft.Extensions.Configuration;
using Diagnostics.DataProviders.KeyVaultCertLoader;
using System.Security.Cryptography.X509Certificates;

namespace Diagnostics.DataProviders
{
    public class CompilerHostCertLoader
    {
        private static readonly Lazy<CompilerHostCertLoader> _instance = new Lazy<CompilerHostCertLoader>(() => new CompilerHostCertLoader());

        public static CompilerHostCertLoader Instance => _instance.Value;

        protected string Thumbprint { get; set; }
        protected string SubjectName { get; set; }
        public X509Certificate2 Cert { get; private set; }

        public void Initialize(IConfiguration configuration)
        {
            if (configuration.GetValue("CompilerHost:UseCertAuth", false))
            {
                Thumbprint = configuration["CompilerHost:CertThumbprint"];
                SubjectName = configuration["CompilerHost:CertSubjectName"];
                Cert = !string.IsNullOrEmpty(SubjectName) ? GenericCertLoader.Instance.GetCertBySubjectName(SubjectName) : GenericCertLoader.Instance.GetCertByThumbprint(Thumbprint);
            }
        }
      
    }
}
