using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Diagnostics.DataProviders
{
    class ArmClient : IGeoMasterClient
    {
        private const string CsmEndpointUrl = "https://management.azure.com/";
        public HttpClient Client { get; }
        public Uri BaseUri { get; }

        public ArmClient(GeoMasterDataProviderConfiguration configuration)
        {
            var handler = new HttpClientHandler();
            BaseUri = new Uri(CsmEndpointUrl);

            Client = new HttpClient(handler)
            {
                BaseAddress = BaseUri
            };

            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration.Token);
            Client.BaseAddress = BaseUri;
        }
    }
}
