using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Diagnostics.Logger;
using Microsoft.Extensions.Configuration;

namespace Diagnostics.DataProviders
{
    internal class GeoMasterCertClient : IGeoMasterClient
    {
        private const int GeoMasterCsmApiPort = 444;
        private const int GeoMasterAdminApiPort = 443;
        private X509Certificate2 _geoMasterCertificate = null;

        public HttpClient Client { get; }
        public Uri BaseUri { get; }

        public IConfiguration Configuration { get; set; }

        public GeoMasterCertClient(GeoMasterDataProviderConfiguration configuration, string geoMasterHostName)
        {
            var handler = new HttpClientHandler();

            if (_geoMasterCertificate == null)
            {
                _geoMasterCertificate = GeoCertLoader.Instance.Cert;
            }

            if (_geoMasterCertificate != null)
            {
                DiagnosticsETWProvider.Instance.LogRuntimeHostMessage("Added cert to GeoMaster Client Handler");
                handler.ClientCertificates.Add(_geoMasterCertificate);
                handler.ServerCertificateCustomValidationCallback = delegate { return true; };
            }

            var geoEndpoint = new UriBuilder(!String.IsNullOrWhiteSpace(geoMasterHostName) ? geoMasterHostName : configuration.GeoEndpointAddress)
            {
                Scheme = "https",
                Port = GeoMasterCsmApiPort
            };

            BaseUri = geoEndpoint.Uri;

            Client = new HttpClient(handler);
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(HeaderConstants.JsonContentType));
            Client.DefaultRequestHeaders.Add(HeaderConstants.UserAgentHeaderName, "appservice-diagnostics");
            Client.Timeout = TimeSpan.FromSeconds(30);
            Client.BaseAddress = BaseUri;
        }
    }
}
