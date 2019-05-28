using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Diagnostics.DataProviders
{
    internal static class KustoClientFactory
    {
        internal static IKustoClient GetKustoClient(KustoDataProviderConfiguration config, string requestId, IHttpClientFactory httpClientFactory)
        {
            if (config.DBName == "Mock")
            {
                return new MockKustoClient();
            }

            return new KustoClient(config, requestId, httpClientFactory);
        }
    }
}
