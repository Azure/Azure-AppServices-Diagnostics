using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;


namespace Diagnostics.DataProviders
{
    public class KustoQuery
    {
        public string Text;
        public string Url;
        public string KustoDesktopUrl;
    }
    public class KustoDataProvider: DiagnosticDataProvider, IDiagnosticDataProvider, IKustoDataProvider
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

        public async Task<DataTable> ExecuteClusterQuery(string query, string requestId = null, string operationName = null)
        {
            return await ExecuteQuery(query, DataProviderConstants.FakeStampForAnalyticsCluster, requestId, operationName);
        }

        public async Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, string operationName = null)
        {           
            await AddQueryInformationToMetadata(query, stampName);
            return await _kustoClient.ExecuteQueryAsync(query, stampName, requestId, operationName);
        }

        public async Task<KustoQuery> GetKustoQuery(string query, string stampName)
        {
            var kustoQuery = await _kustoClient.GetKustoQueryAsync(stampName, query);
            return kustoQuery;
        }

        public DataProviderMetadata GetMetadata()
        {            
            return Metadata;
        }

        private async Task AddQueryInformationToMetadata(string query, string stampName)
        {
            var kustoQuery = await _kustoClient.GetKustoQueryAsync(stampName, query);
            bool queryExists = false;
            
            queryExists = Metadata.PropertyBag.Any(x => x.Key == "Query" &&
                                                        x.Value.GetType() == typeof(KustoQuery) &&
                                                        x.Value.CastTo<KustoQuery>().Url.Equals(kustoQuery.Url, StringComparison.OrdinalIgnoreCase));
            if (!queryExists)
            {
                Metadata.PropertyBag.Add(new KeyValuePair<string, object>("Query", kustoQuery));
            }
        }
    }
}
