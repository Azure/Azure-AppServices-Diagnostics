using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public class KustoQuery
    {
        public string QueryText;
        public string KustoEndpoint;
        public string QueryUrl;
    }
    public class KustoDataProvider: DiagnosticDataProvider, IDiagnosticDataProvider
    {
        private KustoDataProviderConfiguration _configuration;
        private IKustoClient _kustoClient;

        public KustoDataProvider(OperationDataCache cache, KustoDataProviderConfiguration configuration) : base(cache)
        {
            _configuration = configuration;
            _kustoClient = KustoClientFactory.GetKustoClient(configuration);
            Metadata = new DataProviderMetadata
            {
                ProviderName = "Kusto"
            };
        }

        public async Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, [CallerMemberName] string operationName = null)
        {
            AddQueryInformationToMetadata(query);
            return await _kustoClient.ExecuteQueryAsync(query, stampName, requestId, operationName);
        }

        private void AddQueryInformationToMetadata(string query)
        {
            KustoQuery q = new KustoQuery
            {
                QueryText = query,
                KustoEndpoint = "",
                QueryUrl = ""
            };

            Metadata.PropertyBag.Add(new KeyValuePair<string, object>("Query", q));
        }
    }
}
