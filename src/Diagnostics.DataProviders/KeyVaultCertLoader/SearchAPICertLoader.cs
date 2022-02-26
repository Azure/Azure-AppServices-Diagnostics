using System;
using Microsoft.Extensions.Configuration;
using Diagnostics.DataProviders.KeyVaultCertLoader;
using System.Security.Cryptography.X509Certificates;

namespace Diagnostics.DataProviders
{
    public class SearchAPICertLoader
    {
        private static readonly Lazy<SearchAPICertLoader> _instance = new Lazy<SearchAPICertLoader>(() => new SearchAPICertLoader());

        public static SearchAPICertLoader Instance => _instance.Value;

        protected string Thumbprint { get; set; }
        protected string SubjectName { get; set; }
        public X509Certificate2 Cert { get; private set; }

        public void Initialize(IConfiguration configuration)
        {
            if (configuration.GetValue("SearchAPI:UseCertAuth", false))
            {
                Thumbprint = configuration["SearchAPI:CertThumbprint"];
                SubjectName = configuration["SearchAPI:CertSubjectName"];
                Cert = !string.IsNullOrEmpty(SubjectName) ? GenericCertLoader.Instance.GetCertBySubjectName(SubjectName) : GenericCertLoader.Instance.GetCertByThumbprint(Thumbprint);
            }
        }
      
    }
}
