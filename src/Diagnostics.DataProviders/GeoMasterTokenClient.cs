using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Diagnostics.DataProviders
{
    class GeoMasterTokenClient : IGeoMasterClient
    {
        private const string CsmEndpointUrl = "https://management.azure.com/";
        public HttpClient Client { get; }
        private Uri _baseUri{ get; }

        public GeoMasterTokenClient(GeoMasterDataProviderConfiguration configuration)
        {
            var handler = new HttpClientHandler();
            _baseUri = new Uri(CsmEndpointUrl);

            Client = new HttpClient(handler)
            {
                BaseAddress = _baseUri
            };

            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration.Token);
            Client.BaseAddress = _baseUri;
        }
    }
}
