using Microsoft.Extensions.Configuration;
using System;
using Diagnostics.DataProviders.KeyVaultCertLoader;

namespace Diagnostics.DataProviders
{
    public class ContainerAppsMdmCertLoader: CertLoaderBase
    {
        private static readonly Lazy<ContainerAppsMdmCertLoader> _instance = new Lazy<ContainerAppsMdmCertLoader>(() => new ContainerAppsMdmCertLoader());

        public static ContainerAppsMdmCertLoader Instance => _instance.Value;

        protected override string Thumbprint { get; set; }
        protected override string SubjectName { get; set; }

        public void Initialize(IConfiguration configuration)
        {
           Thumbprint = configuration["ContainerAppsMdm:MdmRegistrationCertThumbprint"];
           SubjectName = configuration["ContainerAppsMdm:MdmCertSubjectName"];
           LoadCertFromAppService();
        }
    }
}
