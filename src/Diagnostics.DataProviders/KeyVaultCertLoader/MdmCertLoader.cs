using Microsoft.Extensions.Configuration;
using System;
using Diagnostics.DataProviders.KeyVaultCertLoader;

namespace Diagnostics.DataProviders
{
    public class MdmCertLoader: KeyVaultCertLoaderBase
    {
        private static readonly Lazy<MdmCertLoader> _instance = new Lazy<MdmCertLoader>(() => new MdmCertLoader());

        public static MdmCertLoader Instance => _instance.Value;
       
        protected override string KeyVault { get; set; }
        protected override string CertificateName { get; set; }

        public void Initialize(IConfiguration configuration)
        {
           KeyVault = configuration["Secrets:ProdKeyVaultName"];
           CertificateName = configuration["Mdm:CertificateName"];
           LoadCertFromKeyVault();
        }
    }
}
