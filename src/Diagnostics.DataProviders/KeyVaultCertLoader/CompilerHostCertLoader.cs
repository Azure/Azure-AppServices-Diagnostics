using System;
using Microsoft.Extensions.Configuration;
using Diagnostics.DataProviders.KeyVaultCertLoader;

namespace Diagnostics.DataProviders
{
    public class CompilerHostCertLoader: CertLoaderBase
    {
        private static readonly Lazy<CompilerHostCertLoader> _instance = new Lazy<CompilerHostCertLoader>(() => new CompilerHostCertLoader());

        public static CompilerHostCertLoader Instance => _instance.Value;

        protected override string Thumbprint { get; set; }
        protected override string SubjectName { get; set; }

        public void Initialize(IConfiguration configuration)
        {
            if (configuration.GetValue("CompilerHost:UseCertAuth", false))
            {
                Thumbprint = configuration["CompilerHost:CertThumbprint"];
                SubjectName = configuration["CompilerHost:CertSubjectName"];
                LoadCertFromAppService();
            }
        }
      
    }
}
