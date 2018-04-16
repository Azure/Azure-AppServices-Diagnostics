using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Diagnostics.DataProviders
{
    class GeoMasterCertClient : IGeoMasterClient
    {
        private const int GeoMasterCsmApiPort = 444;
        private const int GeoMasterAdminApiPort = 443;
        private X509Certificate2 _geoMasterCertificate = null;

        public HttpClient Client { get; }
        private Uri _baseUri;

        public GeoMasterCertClient(GeoMasterDataProviderConfiguration configuration)
        {
            var handler = new HttpClientHandler();
            
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            if (_geoMasterCertificate == null)
            {
                _geoMasterCertificate = GetCertificate(configuration.GeoCertThumbprint);
            }
            
            if (_geoMasterCertificate != null)
            {
                handler.ClientCertificates.Add(_geoMasterCertificate);
            }

            var geoEndpoint = new UriBuilder(configuration.GeoEndpointAddress)
            {
                Port = GeoMasterCsmApiPort
            };

            _baseUri = geoEndpoint.Uri;

            Client = new HttpClient(handler)
            {
                BaseAddress = _baseUri
            };

            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
                    throw new InvalidOperationException(String.Format("Cannot find certificate with thumbprint {0}", thumbprint));
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
