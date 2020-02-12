using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;
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

        public KustoDataProvider(OperationDataCache cache, KustoDataProviderConfiguration configuration, string requestId, IKustoHeartBeatService kustoHeartBeat) : base(cache)
        {
            _configuration = configuration;
            _kustoClient = KustoClientFactory.GetKustoClient(configuration, requestId);
            _requestId = requestId;
            _kustoHeartBeatService = kustoHeartBeat;
            _kustoMap = configuration.KustoMap ?? new NullableKustoMap();

            Metadata = new DataProviderMetadata
            {
                ProviderName = "Kusto"
            };
        }

        public async Task<DataTable> ExecuteClusterQuery(string query, string requestId = null, string operationName = null)
        {
            return await ExecuteQuery(query, DataProviderConstants.FakeStampForAnalyticsCluster, requestId, operationName);
        }

        public async Task<DataTable> ExecuteClusterQuery(string query, string cluster, string databaseName, string requestId, string operationName)
        {
            await AddQueryInformationToMetadata(query, cluster, operationName);
            return await _kustoClient.ExecuteQueryAsync(MakeQueryCloudAgnostic(query), _kustoMap.MapCluster(cluster) ?? cluster, _kustoMap.MapDatabase(databaseName) ?? databaseName, requestId, operationName);
        }

        public async Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, string operationName = null)
        {
            var cluster = await GetClusterNameFromStamp(stampName);
            await AddQueryInformationToMetadata(query, cluster, operationName);
            return await _kustoClient.ExecuteQueryAsync(MakeQueryCloudAgnostic(query), _kustoMap.MapCluster(cluster) ?? cluster, _kustoMap.MapDatabase(_configuration.DBName) ?? _configuration.DBName, requestId, operationName);
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
            var kustoQuery = await _kustoClient.GetKustoQueryAsync(MakeQueryCloudAgnostic(query), _kustoMap.MapCluster(cluster) ?? cluster, _kustoMap.MapDatabase(_configuration.DBName) ?? _configuration.DBName, operationName);
            return kustoQuery;
        }

        private string MakeQueryCloudAgnostic(string query)
        {
            var matches = Regex.Matches(query, @"cluster\((?<cluster>(.+))\).database\((?<database>(.+))\)\.");

            if (matches.Any())
            {
                foreach (Match element in matches)
                {
                    if (true)
                    {

                    }
                    query = query.Replace($"cluster({element.Groups["cluster"].Value})", $"cluster('{_kustoMap.MapCluster(element.Groups["cluster"].Value.Trim(new char[] { '\'', '\"' }))}')");
                    query = query.Replace($"database({element.Groups["database"].Value})", $"database('{_kustoMap.MapCluster(element.Groups["database"].Value.Trim(new char[] { '\'', '\"' }))}')");
                }
            }

            return query;
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
            var kustoQuery = await _kustoClient.GetKustoQueryAsync(MakeQueryCloudAgnostic(query), _kustoMap.MapCluster(cluster) ?? cluster, _kustoMap.MapDatabase(_configuration.DBName) ?? _configuration.DBName, operationName);
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

        public async override Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
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
                    description: "Kusto Health Check",
                    kustoException
                    );
            }

            return result;
        }
    }
}
