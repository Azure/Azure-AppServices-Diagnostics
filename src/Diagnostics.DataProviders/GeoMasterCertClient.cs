using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Diagnostics.Logger;

namespace Diagnostics.DataProviders
{
    internal class GeoMasterCertClient : IGeoMasterClient
    {
        private const int GeoMasterCsmApiPort = 444;
        private const int GeoMasterAdminApiPort = 443;
        private X509Certificate2 _geoMasterCertificate = null;
        private static HttpClient _httpClient;

        public HttpClient Client => _httpClient;
        public Uri BaseUri { get; }
        public string AuthenticationToken { get; } // Not used

        public GeoMasterCertClient(GeoMasterDataProviderConfiguration configuration, string geoMasterHostName)
        {
            if (_httpClient == null)
            {
                _httpClient = Init(configuration);
            }

            var geoEndpoint = new UriBuilder(geoMasterHostName)
            {
                Scheme = "https",
                Port = GeoMasterCsmApiPort
            };

            BaseUri = geoEndpoint.Uri;
        }

        public HttpClient Init(GeoMasterDataProviderConfiguration configuration)
        {
            var handler = new HttpClientHandler();

            if (_geoMasterCertificate == null)
            {
                _geoMasterCertificate = GetCertificate(configuration.GeoCertThumbprint);
            }

            if (_geoMasterCertificate != null)
            {
                handler.ClientCertificates.Add(_geoMasterCertificate);
                handler.ServerCertificateCustomValidationCallback = delegate { return true; };
            }

            var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(HeaderConstants.JsonContentType));
            httpClient.DefaultRequestHeaders.Add(HeaderConstants.UserAgentHeaderName, HeaderConstants.UserAgentHeaderValue);
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            return httpClient;
        }

        private X509Certificate2 GetCertificate(string thumbprint)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, true);
                if (certificates.Count == 0)
                {
                    throw new InvalidOperationException($"Cannot find certificate with thumbprint {thumbprint}");
                }

                return certificates[0];
            }
            finally
            {
                if (store != null)
                {
                    store.Close();
                }
            }
        }
    }
}
