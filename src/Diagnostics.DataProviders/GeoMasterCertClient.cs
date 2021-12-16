using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Diagnostics.Logger;
using Microsoft.Extensions.Configuration;

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

        public IConfiguration Configuration { get; set; }

        public GeoMasterCertClient(GeoMasterDataProviderConfiguration configuration, string geoMasterHostName)
        {
            if (_httpClient == null)
            {
                _httpClient = Init(configuration);
            }

            var geoEndpoint = new UriBuilder(geoMasterHostName)
            {
                Scheme = "https",
            };

            // Use the default CSM port if a port number isn't specified at the end
            if (!Regex.IsMatch(geoMasterHostName, @":\d+/?$"))
            {
                geoEndpoint.Port = GeoMasterCsmApiPort;
            }

            BaseUri = geoEndpoint.Uri;
        }

        public HttpClient Init(GeoMasterDataProviderConfiguration configuration)
        {
            var handler = new HttpClientHandler();

            if (_geoMasterCertificate == null)
            {
                _geoMasterCertificate = GeoCertLoader.Instance.Cert;
            }

            if (_geoMasterCertificate != null)
            {
                handler.ClientCertificates.Add(_geoMasterCertificate);
                handler.ServerCertificateCustomValidationCallback = delegate { return true; };
            }

            var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(HeaderConstants.JsonContentType));
            httpClient.DefaultRequestHeaders.Add(HeaderConstants.UserAgentHeaderName, "appservice-diagnostics");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            return httpClient;
        }
    }
}
