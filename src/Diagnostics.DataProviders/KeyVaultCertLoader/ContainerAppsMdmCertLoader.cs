using Microsoft.Extensions.Configuration;
using System;
using Diagnostics.DataProviders.KeyVaultCertLoader;
using System.Security.Cryptography.X509Certificates;

namespace Diagnostics.DataProviders
{
    public class ContainerAppsMdmCertLoader
    {
        private static readonly Lazy<ContainerAppsMdmCertLoader> _instance = new Lazy<ContainerAppsMdmCertLoader>(() => new ContainerAppsMdmCertLoader());

        public static ContainerAppsMdmCertLoader Instance => _instance.Value;

        protected string Thumbprint { get; set; }
        protected string SubjectName { get; set; }

        public X509Certificate2 Cert { get; private set; }

        public void Initialize(IConfiguration configuration)
        {
           Thumbprint = configuration["ContainerAppsMdm:MdmRegistrationCertThumbprint"];
           SubjectName = configuration["ContainerAppsMdm:MdmCertSubjectName"];
           Cert = !string.IsNullOrEmpty(SubjectName) ? GenericCertLoader.Instance.GetCertBySubjectName(SubjectName) : GenericCertLoader.Instance.GetCertByThumbprint(Thumbprint);
        }
    }
}
