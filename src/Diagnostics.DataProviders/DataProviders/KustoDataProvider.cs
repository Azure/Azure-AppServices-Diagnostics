using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders
{
    public class KustoQuery
    {
        public string Text;
        public string Url;
        public string KustoDesktopUrl;
    }

    public class KustoDataProvider : DiagnosticDataProvider, IDiagnosticDataProvider, IKustoDataProvider
    {
        private KustoDataProviderConfiguration _configuration;
        private IKustoClient _kustoClient;
        private string _requestId;
        private IKustoHeartBeatService _kustoHeartBeatService;

        public KustoDataProvider(OperationDataCache cache, KustoDataProviderConfiguration configuration, string requestId, IKustoHeartBeatService kustoHeartBeat) : base(cache)
        {
            _configuration = configuration;
            _kustoClient = KustoClientFactory.GetKustoClient(configuration, requestId);
            _requestId = requestId;
            _kustoHeartBeatService = kustoHeartBeat;
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
            var cluster = GetClusterNameFromStamp(stampName);
            await AddQueryInformationToMetadata(query, cluster);
            return await _kustoClient.ExecuteQueryAsync(query, cluster, _configuration.DBName, requestId, operationName);
        }

        public Task<KustoQuery> GetKustoClusterQuery(string query)
        {
            return GetKustoQuery(query, DataProviderConstants.FakeStampForAnalyticsCluster);
        }

        public async Task<KustoQuery> GetKustoQuery(string query, string stampName)
        {
            var cluster = GetClusterNameFromStamp(stampName);
            var kustoQuery = await _kustoClient.GetKustoQueryAsync(query, cluster, _configuration.DBName);
            return kustoQuery;
        }

        internal async Task<DataTable> ExecuteQueryForHeartbeat(string query, string cluster, int timeoutSeconds, string requestId = null, string operationName = null)
        {
            return await _kustoClient.ExecuteQueryAsync(query, cluster, _configuration.DBName, timeoutSeconds, requestId, operationName);
        }

        public DataProviderMetadata GetMetadata()
        {
            return Metadata;
        }

        private async Task AddQueryInformationToMetadata(string query, string cluster)
        {
            var kustoQuery = await _kustoClient.GetKustoQueryAsync(query, cluster, _configuration.DBName);
            bool queryExists = false;

            queryExists = Metadata.PropertyBag.Any(x => x.Key == "Query" &&
                                                        x.Value.GetType() == typeof(KustoQuery) &&
                                                        x.Value.CastTo<KustoQuery>().Url.Equals(kustoQuery.Url, StringComparison.OrdinalIgnoreCase));
            if (!queryExists)
            {
                Metadata.PropertyBag.Add(new KeyValuePair<string, object>("Query", kustoQuery));
            }
        }

        private string GetClusterNameFromStamp(string stampName)
        {
            return _kustoHeartBeatService.GetClusterNameFromStamp(stampName);
        }
    }
}
