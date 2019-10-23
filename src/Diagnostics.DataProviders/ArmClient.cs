using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Diagnostics.DataProviders
{
    internal class ArmClient : IGeoMasterClient
    {
        private const string CsmEndpointUrl = "https://management.azure.com/";
        private static readonly HttpClient _client = new HttpClient();
        public HttpClient Client => _client;
        public Uri BaseUri => _client.BaseAddress;
        public string AuthenticationToken { get; }

        static ArmClient()
        {
            _client.BaseAddress = new Uri(CsmEndpointUrl);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public ArmClient(GeoMasterDataProviderConfiguration configuration)
        {
            if (configuration.Token == null)
            {
                throw new ArgumentException("configuration.Token is null");
            }
            this.AuthenticationToken = configuration.Token;
        }
    }
}
