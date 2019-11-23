using Microsoft.Extensions.Configuration;
using System;
using Diagnostics.DataProviders.KeyVaultCertLoader;

namespace Diagnostics.DataProviders
{
    public class MdmCertLoader: CertLoaderBase
    {
        private static readonly Lazy<MdmCertLoader> _instance = new Lazy<MdmCertLoader>(() => new MdmCertLoader());

        public static MdmCertLoader Instance => _instance.Value;

        protected override string Thumbprint { get; set; }

        public void Initialize(IConfiguration configuration)
        {
           Thumbprint = configuration["Mdm:MdmRegistrationCertThumbprint"];
           LoadCertFromAppService();
        }
    }
}
