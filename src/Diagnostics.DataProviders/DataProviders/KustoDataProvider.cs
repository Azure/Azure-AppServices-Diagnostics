using Diagnostics.ModelsAndUtils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public class KustoDataProvider: DiagnosticDataProvider, IDiagnosticDataProvider
    {
        private KustoDataProviderConfiguration _configuration;
        private IKustoClient _kustoClient;

        public KustoDataProvider(OperationDataCache cache, KustoDataProviderConfiguration configuration): base(cache)
        {
            _configuration = configuration;
            _kustoClient = KustoClientFactory.GetKustoClient(configuration);
        }

        public async Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, string operationName = null)
        {
            return await _kustoClient.ExecuteQueryAsync(query, stampName, requestId, operationName);
        }
    }
}
