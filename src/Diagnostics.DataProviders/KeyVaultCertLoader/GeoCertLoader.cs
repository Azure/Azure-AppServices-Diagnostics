using System;
using Microsoft.Extensions.Configuration;
using Diagnostics.DataProviders.KeyVaultCertLoader;
using Diagnostics.Logger;

namespace Diagnostics.DataProviders
{
    public class GeoCertLoader: KeyVaultCertLoaderBase
    {
        private static readonly Lazy<GeoCertLoader> _instance = new Lazy<GeoCertLoader>(() => new GeoCertLoader());

        public static GeoCertLoader Instance => _instance.Value;

        protected override string KeyVault { get; set; }
        protected override string CertificateName { get; set; }

        public void Initialize(IConfiguration configuration)
        {
            KeyVault = configuration["Secrets:ProdKeyVaultName"];
            CertificateName = configuration["GeoMaster:CertificateName"];
            DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Loading cert {CertificateName} from key vault {KeyVault}");
            LoadCertFromKeyVault();
        }
      
    }
}
