using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
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

        public KustoDataProvider(OperationDataCache cache, KustoDataProviderConfiguration configuration, string requestId, IHttpClientFactory httpClientFactory) : base(cache)
        {
            _configuration = configuration;
            _kustoClient = KustoClientFactory.GetKustoClient(configuration, requestId, httpClientFactory);
            _requestId = requestId;
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
            var cluster = GetClusterNameFromStamp(stampName);
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

        public DataProviderMetadata GetMetadata()
        {
            return Metadata;
        }

        private async Task AddQueryInformationToMetadata(string query, string stampName)
        {
            var cluster = GetClusterNameFromStamp(stampName);
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
            string kustoClusterName = null;
            string appserviceRegion = ParseRegionFromStamp(stampName);

            if (!_configuration.RegionSpecificClusterNameCollection.TryGetValue(appserviceRegion.ToLower(), out kustoClusterName))
            {
                if (!_configuration.RegionSpecificClusterNameCollection.TryGetValue("*", out kustoClusterName))
                {
                    throw new KeyNotFoundException(String.Format("Kusto Cluster Name not found for Region : {0}", appserviceRegion.ToLower()));
                }
            }
            return kustoClusterName;
        }

        private static string ParseRegionFromStamp(string stampName)
        {
            if (string.IsNullOrWhiteSpace(stampName))
            {
                throw new ArgumentNullException(nameof(stampName));
            }

            var stampParts = stampName.Split(new char[] { '-' });
            if (stampParts.Any() && stampParts.Length >= 3)
            {
                return stampParts[2];
            }

            //return * for private stamps if no prod stamps are found
            return "*";
        }
    }
}
