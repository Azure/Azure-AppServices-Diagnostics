using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.DataProviders.Utility;
using Diagnostics.ModelsAndUtils.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Diagnostics.DataProviders
{
    public class KustoQuery
    {
        public string Text;
        public string Url;
        public string KustoDesktopUrl;
        public string OperationName;
    }

    public class KustoDataProvider : DiagnosticDataProvider, IKustoDataProvider
    {
        private KustoDataProviderConfiguration _configuration;
        private IKustoClient _kustoClient;
        private string _requestId;
        private IKustoHeartBeatService _kustoHeartBeatService;
        private IKustoMap _kustoMap;

        public KustoDataProvider(OperationDataCache cache, KustoDataProviderConfiguration configuration, string requestId, IKustoHeartBeatService kustoHeartBeat) : base(cache, configuration)
        {
            var publicClouds = new string[] { DataProviderConstants.AzureCloud, DataProviderConstants.AzureCloudAlternativeName };
            _configuration = configuration;
            _kustoClient = KustoClientFactory.GetKustoClient(configuration, requestId);
            _requestId = requestId;
            _kustoHeartBeatService = kustoHeartBeat;
            _kustoMap = new NullableKustoMap();

            if (publicClouds.Any(s => configuration.CloudDomain.Equals(s, StringComparison.CurrentCultureIgnoreCase)))
            {
                if (configuration.UseKustoMapForPublic)
                {
                    _kustoMap = configuration.KustoMap ?? new NullableKustoMap();
                }
            }
            else
            {
                _kustoMap = configuration.KustoMap ?? new NullableKustoMap();
            }

            Metadata = new DataProviderMetadata
            {
                ProviderName = "Kusto"
            };
        }

        private bool IsPublicCloud
        {
            get {
                var publicClouds = new string[] { DataProviderConstants.AzureCloud, DataProviderConstants.AzureCloudAlternativeName };
                return publicClouds.Any(s => _configuration.CloudDomain.Equals(s, StringComparison.CurrentCultureIgnoreCase));
            }
        }

        public async Task<DataTable> ExecuteClusterQuery(string query, string requestId = null, string operationName = null)
        {
            if(!query.Contains("geneva_metrics_request"))
            {
                //Allow partner KQL queries to proxy through us
                var matches = Regex.Matches(query, @"cluster\((?<cluster>([^\)]+))\).database\((?<database>([^\)]+))\)\.");
                if (matches.Any())
                {
                    foreach (Match element in matches)
                    {
                        string targetCluster = element.Groups["cluster"].Value.Trim(new char[] { '\'', '\"' });
                        string targetDatabase = element.Groups["database"].Value.Trim(new char[] { '\'', '\"' });

                        if (!string.IsNullOrWhiteSpace(targetCluster) && !string.IsNullOrWhiteSpace(targetDatabase))
                        {
                            targetCluster = targetCluster.Replace(".kusto.windows.net", string.Empty, StringComparison.OrdinalIgnoreCase);
                            return await ExecuteClusterQuery(query, targetCluster, targetDatabase, requestId, operationName);
                        }
                    }
                }
            }            

            return await ExecuteQuery(query, DataProviderConstants.FakeStampForAnalyticsCluster, requestId, operationName);
        }

        public async Task<DataTable> ExecuteClusterQuery(string query, string cluster, string databaseName, string requestId, string operationName)
        {
            await AddQueryInformationToMetadata(query, cluster, operationName);
            return await _kustoClient.ExecuteQueryAsync(Helpers.MakeQueryCloudAgnostic(_kustoMap, query), _kustoMap.MapCluster(cluster) ?? cluster, _kustoMap.MapDatabase(databaseName) ?? databaseName, requestId, operationName);
        }

        public async Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, string operationName = null)
        {
            var cluster = await GetClusterNameFromStamp(stampName);
            await AddQueryInformationToMetadata(query, cluster, operationName);
            return await _kustoClient.ExecuteQueryAsync(Helpers.MakeQueryCloudAgnostic(_kustoMap, query), _kustoMap.MapCluster(cluster) ?? cluster, _kustoMap.MapDatabase(_configuration.DBName) ?? _configuration.DBName, requestId, operationName);
        }

        public async Task<DataTable> ExecuteQueryOnAllAppAppServiceClusters(string query, string operationName)
        {
            if (string.IsNullOrWhiteSpace(operationName))
            {
                throw new ArgumentNullException(nameof(operationName), "OperationName cannot be empty. Please supply a name to identify the query.");
            }
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentNullException(nameof(query), "Query cannot be empty. Please supply a query to execute.");
            }

            List<Task<DataTable>> queryTask = new List<Task<DataTable>>();

            if(IsPublicCloud)
            {
                queryTask.Add(ExecuteQuery(query, "waws-prod-bay-153", null, operationName));
                queryTask.Add(ExecuteQuery(query, "waws-prod-blu-189", null, operationName));
                queryTask.Add(ExecuteQuery(query, "waws-prod-dm1-187", null, operationName));
                queryTask.Add(ExecuteQuery(query, "waws-prod-am2-329", null, operationName));
                queryTask.Add(ExecuteQuery(query, "waws-prod-db3-169", null, operationName));
                queryTask.Add(ExecuteQuery(query, "waws-prod-hk1-029", null, operationName));
            }
            else
            {
                queryTask.Add(ExecuteQuery(query, DataProviderConstants.FakeStampForAnalyticsCluster, null, operationName));
            }

            var queryResult = await Task.WhenAll(queryTask.ToArray());
            DataTable mergedTable = new DataTable();
            foreach (DataTable dt in queryResult)
            {
                if (dt.Rows.Count > 0)
                {
                    if(dt.Rows.Count > 3000)
                    {
                        throw new Exception($"Query {operationName} returned more than 3000 rows. Please modify the query to fetch fewer rows while running across all app service clusters.");
                    }
                    mergedTable.Merge(dt, false, MissingSchemaAction.Add);
                }
            }
            return mergedTable;

        }

        public Task<KustoQuery> GetKustoClusterQuery(string query)
        {
            return GetKustoQuery(query, DataProviderConstants.FakeStampForAnalyticsCluster);
        }

        public Task<KustoQuery> GetKustoQuery(string query, string stampName)
        {
            return GetKustoQuery(query, stampName, null);
        }

        public async Task<KustoQuery> GetKustoQuery(string query, string stampName, string operationName)
        {
            var cluster = await GetClusterNameFromStamp(stampName);
            var kustoQuery = await _kustoClient.GetKustoQueryAsync(Helpers.MakeQueryCloudAgnostic(_kustoMap, query), _kustoMap.MapCluster(cluster) ?? cluster, _kustoMap.MapDatabase(_configuration.DBName) ?? _configuration.DBName, operationName);
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

        private async Task AddQueryInformationToMetadata(string query, string cluster, string operationName = null)
        {
            var kustoQuery = await _kustoClient.GetKustoQueryAsync(Helpers.MakeQueryCloudAgnostic(_kustoMap, query), _kustoMap.MapCluster(cluster) ?? cluster, _kustoMap.MapDatabase(_configuration.DBName) ?? _configuration.DBName, operationName);
            bool queryExists = false;

            queryExists = Metadata.PropertyBag.Any(x => x.Key == "Query" &&
                                                        x.Value.GetType() == typeof(KustoQuery) &&
                                                        x.Value.CastTo<KustoQuery>().Url.Equals(kustoQuery.Url, StringComparison.OrdinalIgnoreCase));
            if (!queryExists)
            {
                Metadata.PropertyBag.Add(new KeyValuePair<string, object>("Query", kustoQuery));
            }
        }

        private async Task<string> GetClusterNameFromStamp(string stampName)
        {
            return await _kustoHeartBeatService.GetClusterNameFromStamp(stampName);
        }

        public async override Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            DataTable response;
            Exception kustoException = null;
            HealthCheckResult result;
            try
            {
                var cluster = _configuration.RegionSpecificClusterNameCollection.Values.First();
                response = await ExecuteClusterQuery(_configuration.HeartBeatQuery, cluster, _configuration.DBName, null, null);
            }
            catch (Exception ex)
            {
                kustoException = ex;
            }
            finally
            {
                result = new HealthCheckResult(
                    kustoException == null ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                    "Kusto",
                    "Run sample kusto queries",
                    kustoException
                    );
            }

            return result;
        }
    }
}
