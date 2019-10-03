using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Kusto.Data;
using Kusto.Data.Net.Client;
using Kusto.Data.Common;
using Kusto.Cloud.Platform.Data;

namespace Diagnostics.DataProviders
{
    class KustoSDKClient : IKustoClient
    {
        public async Task<DataTable> ExecuteQueryAsync(string query, string cluster, string database, int timeoutSeconds, string requestId = null, string operationName = null)
        {
            throw new NotImplementedException();
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, string cluster, string database, string requestId = null, string operationName = null)
        {
            throw new NotImplementedException();
        }

        public async Task<KustoQuery> GetKustoQueryAsync(string query, string cluster, string database)
        {
            throw new NotImplementedException();
        }

        public async Task<KustoQuery> GetKustoQueryAsync(string query, string cluster, string database, string operationName = null)
        {
            throw new NotImplementedException();
        }

        public KustoClient(KustoDataProviderConfiguration config, string requestId)
        {
            _requestId = requestId;
            KustoApiQueryEndpoint = config.KustoApiEndpoint + ":443/v1/rest/query";
            FailoverClusterMapping = config.FailoverClusterNameCollection;
        }
    }
}
