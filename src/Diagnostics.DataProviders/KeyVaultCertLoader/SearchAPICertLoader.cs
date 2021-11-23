using System;
using Microsoft.Extensions.Configuration;
using Diagnostics.DataProviders.KeyVaultCertLoader;

namespace Diagnostics.DataProviders
{
    public class SearchAPICertLoader: CertLoaderBase
    {
        private static readonly Lazy<SearchAPICertLoader> _instance = new Lazy<SearchAPICertLoader>(() => new SearchAPICertLoader());

        public static SearchAPICertLoader Instance => _instance.Value;

        protected override string Thumbprint { get; set; }
        protected override string SubjectName { get; set; }

        public void Initialize(IConfiguration configuration)
        {
            if (configuration.GetValue("SearchAPI:UseCertAuth", false))
            {
                Thumbprint = configuration["SearchAPI:CertThumbprint"];
                SubjectName = configuration["SearchAPI:CertSubjectName"];
                LoadCertFromAppService();
            }
        }
      
    }
}
