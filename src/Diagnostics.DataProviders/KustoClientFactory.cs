using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders
{
    internal static class KustoClientFactory
    {
        internal static IKustoClient GetKustoClient(KustoDataProviderConfiguration config, string requestId)
        {
            if (config.DBName == "Mock")
            {
                return new MockKustoClient();
            }

            return new KustoClient(requestId);
        }
    }
}
