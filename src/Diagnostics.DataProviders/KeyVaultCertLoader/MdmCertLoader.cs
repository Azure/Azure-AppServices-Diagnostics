using Diagnostics.DataProviders.KeyVaultCertLoader;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Diagnostics.DataProviders
{
    public class MdmCertLoader
    {
        private static readonly Lazy<MdmCertLoader> _instance = new Lazy<MdmCertLoader>(() => new MdmCertLoader());

        public static MdmCertLoader Instance => _instance.Value;

        protected string Thumbprint { get; set; }
        protected string SubjectName { get; set; }

        public X509Certificate2 Cert { get; private set; }

        public void Initialize(IConfiguration configuration)
        {
           Thumbprint = configuration["Mdm:MdmRegistrationCertThumbprint"];
           SubjectName = configuration["Mdm:MdmCertSubjectName"];
           Cert = !string.IsNullOrEmpty(SubjectName) ? GenericCertLoader.Instance.GetCertBySubjectName(SubjectName) : GenericCertLoader.Instance.GetCertByThumbprint(Thumbprint);
        }
    }
}
