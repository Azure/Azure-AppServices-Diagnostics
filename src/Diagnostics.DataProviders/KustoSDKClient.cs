using System;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Cloud.Platform.Data;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Web;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;
using Diagnostics.DataProviders.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Diagnostics.DataProviders
{
    public class KustoSDKClient : IKustoClient
    {
        private readonly KustoDataProviderConfiguration _config;
        private string _requestId;
        private string _kustoApiQueryEndpoint;
        private string _appKey;
        private string _clientId;
        private string _aadAuthority;
        private static ConcurrentDictionary<Tuple<string, string>, ICslQueryProvider> QueryProviderMapping;
        private static List<string> exceptionsToRetryFor = new List<string>();

        /// <summary>
        /// Failover cluster mapping
        /// </summary>
        private ConcurrentDictionary<string, string> FailoverClusterMapping { get; set; }

        public KustoSDKClient(KustoDataProviderConfiguration config, string requestId)
        {
            if (QueryProviderMapping == null)
            {
                QueryProviderMapping = new ConcurrentDictionary<Tuple<string, string>, ICslQueryProvider>();
            }

            _config = config;
            _requestId = requestId;
            _kustoApiQueryEndpoint = config.KustoApiEndpoint + ":443";
            _appKey = config.AppKey;
            _clientId = config.ClientId;
            _aadAuthority = config.AADAuthority;
            FailoverClusterMapping = config.FailoverClusterNameCollection;

            if (!string.IsNullOrWhiteSpace(_config.ExceptionsToRetryFor))
            {
                exceptionsToRetryFor = _config.ExceptionsToRetryFor.Split(DataProviderConstants.CommonSeparationChars).Select(p => p.Trim()).ToList();
            }
        }

        private ICslQueryProvider Client(string cluster, string database)
        {
            var key = Tuple.Create(cluster, database);
            if (!QueryProviderMapping.ContainsKey(key))
            {
                KustoConnectionStringBuilder connectionStringBuilder = new KustoConnectionStringBuilder(_kustoApiQueryEndpoint.Replace("{cluster}", cluster), database)
                {
                    FederatedSecurity = true,
                    ApplicationClientId = _clientId,
                    ApplicationKey = _appKey,
                    Authority = _aadAuthority
                };

                var queryProvider = Kusto.Data.Net.Client.KustoClientFactory.CreateCslQueryProvider(connectionStringBuilder);
                if(!QueryProviderMapping.TryAdd(key, queryProvider))
                {
                    queryProvider.Dispose();
                }
            }

            return QueryProviderMapping[key];
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, string cluster, string database, int timeoutSeconds, string requestId = null, string operationName = null, DateTime? startTime = null, DateTime? endTime = null)
        {
            var timeTakenStopWatch = new Stopwatch();
            DataSet dataSet = null;
            ClientRequestProperties clientRequestProperties = new ClientRequestProperties();
            var kustoClientId = $"Diagnostics.{operationName ?? "Query"};{_requestId};{startTime?.ToString() ?? "UnknownStartTime"};{endTime?.ToString() ?? "UnknownEndTime"}##{0}_{Guid.NewGuid().ToString()}";
            clientRequestProperties.ClientRequestId = kustoClientId;
            clientRequestProperties.SetOption("servertimeout", new TimeSpan(0,0,timeoutSeconds));
            if(cluster.StartsWith("waws",StringComparison.OrdinalIgnoreCase) && cluster != "wawscusdiagleadertest1.centralus" 
                && !cluster.Equals("wawscusaggdiagleader.centralus", StringComparison.OrdinalIgnoreCase))
            {
                clientRequestProperties.SetOption(ClientRequestProperties.OptionQueryConsistency, ClientRequestProperties.OptionQueryConsistency_Weak);
            }
            try
            {
                timeTakenStopWatch.Start();
                var kustoClient = Client(cluster, database);
                Task<IDataReader> kustoTask = null;

                if (_config.QueryShadowingClusterMapping != null && _config.QueryShadowingClusterMapping.TryGetValue(cluster, out var shadowClusters))
                {
                    if (query != _config.HeartBeatQuery)
                    {
                        foreach (string shadowCluster in shadowClusters)
                        {
                            try
                            {
                                var shadowClientRequestProperties = clientRequestProperties;
                                var shadowKustoClient = Client(shadowCluster, database);
                                kustoTask = shadowKustoClient.ExecuteQueryAsync(database, query, shadowClientRequestProperties)
                                    .ContinueWith(t =>
                                    {
                                        if (t.IsFaulted)
                                        {
                                            // generate a new client id and retry
                                            kustoClientId = $"Diagnostics.{operationName ?? "Query"};{_requestId};{startTime?.ToString() ?? "UnknownStartTime"};{endTime?.ToString() ?? "UnknownEndTime"}##{0}_{Guid.NewGuid().ToString()}";
                                            LogKustoQuery(query, shadowCluster, operationName, timeTakenStopWatch, kustoClientId, t.Exception, null);
                                            return kustoClient.ExecuteQueryAsync(database, query, clientRequestProperties);
                                        }
                                        else
                                        {
                                            cluster = shadowCluster; // for LogKustoQuery
                                            return t;
                                        }
                                    }).Unwrap();
                            }
                            catch (Exception e)
                            {
                                DiagnosticsETWProvider.Instance.LogRuntimeHostHandledException(
                                    requestId ?? string.Empty,
                                    "ExecuteQueryAsyncOnTestCluster",
                                    string.Empty,
                                    string.Empty,
                                    string.Empty,
                                    e.GetType().ToString(),
                                    JsonConvert.SerializeObject(e));
                            }
                        }
                    }
                }

                if(kustoTask == null)
                {
                    kustoTask = kustoClient.ExecuteQueryAsync(database, query, clientRequestProperties);
                }
                var result = await kustoTask;
                dataSet = result.ToDataSet();
            }
            catch (Exception ex)
            {
                timeTakenStopWatch.Stop();
                LogKustoQuery(query, cluster, operationName, timeTakenStopWatch, kustoClientId, ex, dataSet);
                if (ex is Kusto.Data.Exceptions.SyntaxException)
                {
                    if (query != null && query.Contains("Tenant in ()"))
                    {
                        throw new KustoTenantListEmptyException("KustoDataProvider", "Malformed Query: Query contains an empty tenant list.");
                    }
                }

                throw;
            }
            finally
            {
                timeTakenStopWatch.Stop();
            }

            LogKustoQuery(query, cluster, operationName, timeTakenStopWatch, kustoClientId, null, dataSet);

            var datatable = dataSet?.Tables?[0];
            if (datatable == null)
            {
                datatable = new DataTable();
            }
            return datatable;
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, string cluster, string database, string requestId = null, string operationName = null, DateTime? startTime = null, DateTime? endTime = null)
        {
            int attempt = 0;
            string source = string.IsNullOrWhiteSpace(operationName) ? "KustoSDKClient_ExecuteQueryAsync" : operationName;

            DateTime invocationStartTime = default;
            DateTime invocationEndTime = default;
            Task<DataTable> executeQueryTask = default;
            DataTable dtResult = default;
            Exception attemptException = default;
            var exceptions = new List<Exception>();
            bool isConditionMetForRetryAgainstLeaderCluster = false;

            do
            {
                try
                {
                    //figure out how to use the new config settings here
                    if (attempt == _config.MaxRetryCount && _config.MaxRetryCount != 0 && _config.UseBackupClusterForLastRetryAttempt || isConditionMetForRetryAgainstLeaderCluster)
                    {
                        // Last Retry Attempt
                        // Switch to backup cluster since useBackupClusterForLastAttempt is set to true.
                        cluster = GetBackupClusterName(cluster);
                    }

                    DiagnosticsETWProvider.Instance.LogRetryAttemptMessage(
                        requestId ?? string.Empty,
                        source,
                        $"Starting Attempt : {attempt}, Cluster: {cluster}, Query : {query}"
                        );

                    invocationStartTime = DateTime.UtcNow;
                    attemptException = null;
                    executeQueryTask = ExecuteQueryAsync(query, cluster, database, DataProviderConstants.DefaultTimeoutInSeconds, requestId, operationName, startTime, endTime);
                    dtResult = await executeQueryTask;
                }
                catch (Exception ex)
                {
                    dtResult = default;
                    attemptException = ex;
                    exceptions.Add(ex);
                }
                finally
                {
                    invocationEndTime = DateTime.UtcNow;
                    var totalResponseTime = invocationEndTime - invocationStartTime;
                    string exceptionType = attemptException != null ? attemptException.GetType().ToString() : string.Empty;
                    string exceptionDetails = attemptException != null ? attemptException.ToString() : string.Empty;

                    DiagnosticsETWProvider.Instance.LogRetryAttemptSummary(
                            requestId ?? string.Empty,
                            source,
                            $"Attempt : {attempt},  IsSuccessful : {attemptException == null}, Cluster: {cluster}, Query : {query}",
                            Convert.ToInt64(totalResponseTime.TotalMilliseconds),
                            invocationStartTime.ToString("HH:mm:ss.fff"),
                            invocationEndTime.ToString("HH:mm:ss.fff"),
                            exceptionType,
                            exceptionDetails
                            );

                    if (attemptException != null && IsOverridableExceptionsToRetryAgainstLeaderCluster(attemptException) && totalResponseTime.TotalSeconds <= (double)_config.OverridableExceptionsToRetryAgainstLeaderCluster.Single(x => attemptException.Message.ToLower().Contains(x[0].ToString().ToLower()))[1])

                    {
                        isConditionMetForRetryAgainstLeaderCluster = true;
                    }
                    // Logic to check if retry needs to continue
                    else if (totalResponseTime.TotalSeconds > _config.MaxFailureResponseTimeInSecondsForRetry || !IsExceptionRetryable(attemptException))
                    {
                        DiagnosticsETWProvider.Instance.LogRetryAttemptMessage(
                        requestId ?? string.Empty,
                        source,
                        $"Not continuing Retries after Attempt : {attempt}, Cluster: {cluster}, Query : {query}"
                        );

                        attempt = _config.MaxRetryCount;
                    }

                    attempt++;
                }

                if (attempt > 1 && isConditionMetForRetryAgainstLeaderCluster) break;
                else if (attempt <= _config.MaxRetryCount && !isConditionMetForRetryAgainstLeaderCluster) await Task.Delay(_config.RetryDelayInSeconds * 1000);

            } while (attempt <= _config.MaxRetryCount);

            if (executeQueryTask.IsCompletedSuccessfully && dtResult != default)
            {
                return dtResult;
            }

            throw new AggregateException($"{source} failed all attempts. Look at inner exceptions", exceptions);
        }

        public async Task<KustoQuery> GetKustoQueryAsync(string query, string cluster, string database)
        {
            return await GetKustoQueryAsync(query, cluster, database, null);
        }

        public async Task<KustoQuery> GetKustoQueryAsync(string query, string cluster, string database, string operationName = null)
        {
            try
            {
                var encodedQuery = await EncodeQueryAsBase64UrlAsync(query);
                if(FailoverClusterMapping.ContainsKey(cluster))
                {
                    cluster = FailoverClusterMapping[cluster];
                }
                var kustoQuery = new KustoQuery
                {
                    Text = query,
                    Url = $"https://dataexplorer.azure.com/clusters/{cluster}/databases/{database}?query={encodedQuery}",
                    KustoDesktopUrl = $"https://{cluster}.kusto.windows.net:443/{database}?query={encodedQuery}&web=0",
                    OperationName = operationName
                };
                return kustoQuery;
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogDataProviderMessage(_requestId, "KustoClient", $"GetKustoQueryAsync Failure. Query: {query}; Cluster: {cluster}; Database: {database}; Exception: {ex.ToString()}");
                throw;
            }
        }

        private async Task<string> EncodeQueryAsBase64UrlAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return string.Empty;
            }

            var bytes = Encoding.UTF8.GetBytes(query);
            string result;
            using (var memoryStream = new MemoryStream())
            {
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                {
                    await gZipStream.WriteAsync(bytes, 0, bytes.Length);
                }

                memoryStream.Seek(0L, SeekOrigin.Begin);
                result = HttpUtility.UrlEncode(Convert.ToBase64String(memoryStream.ToArray()));
            }

            return result;
        }

        private void LogKustoQuery(string query, string cluster, string operationName, Stopwatch timeTakenStopWatch, string kustoClientId, Exception kustoApiException, DataSet dataSet, string kustoResponse = "")
        {
            var status = kustoApiException == null ? "Success" : "Failed";

            kustoResponse = (!string.IsNullOrWhiteSpace(kustoResponse)) ? $" {Environment.NewLine} KustoResponseBody : {kustoResponse} " : string.Empty;

            object stats = null;
            if (dataSet != null && dataSet.Tables != null && dataSet.Tables.Count >= 4)
            {
                var statisticsTable = dataSet.Tables[dataSet.Tables.Count - 2].ToDataTableResponseObject();
                if (statisticsTable.Rows.Length >= 2 && statisticsTable.Rows[1].Length >= 5)
                {
                    stats = statisticsTable.Rows[1][4];
                }
            }

            DiagnosticsETWProvider.Instance.LogKustoQueryInformation(
               operationName ?? "None",
               _requestId,
               $"KustoQueryRequestId:{kustoClientId},Status:{status},TimeTaken:{timeTakenStopWatch.ElapsedMilliseconds},Cluster:{cluster}",
               timeTakenStopWatch.ElapsedMilliseconds,
               JsonConvert.SerializeObject(stats) ?? string.Empty,
               query,
               kustoApiException != null ? kustoApiException.GetType().ToString() : string.Empty,
               kustoApiException != null ? $"{kustoApiException.ToString()}{kustoResponse}" : string.Empty);
        }

        /// <summary>
        /// Fetches backup cluster name for a given cluster.
        /// If there is no backup cluster, it returns the same cluster.
        /// </summary>
        /// <param name="cluster">cluster name</param>
        /// <returns>Backup cluster name</returns>
        private string GetBackupClusterName(string cluster)
        {
            string backupCluster = cluster;

            if (string.IsNullOrWhiteSpace(cluster))
            {
                return backupCluster;
            }

            if (cluster.EndsWith(DataProviderConstants.kustoFollowerClusterSuffix, StringComparison.OrdinalIgnoreCase))
            {
                backupCluster = cluster.Trim().Replace(DataProviderConstants.kustoFollowerClusterSuffix, string.Empty, StringComparison.OrdinalIgnoreCase);
            }
            else if (FailoverClusterMapping.Keys.Contains(cluster))
            {
                backupCluster = FailoverClusterMapping[cluster];
            }

            return backupCluster;
        }

        /// <summary>
        /// Indicates if an request with exception can be retried
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <returns>true, if the request can be retried</returns>
        private bool IsExceptionRetryable(Exception ex)
        {
            if (ex is null)
            {
                return false;
            }

            return exceptionsToRetryFor.Exists(item => ex.ToString().ToLower().Contains(item.ToLower()));
        }

        private bool IsOverridableExceptionsToRetryAgainstLeaderCluster(Exception ex)
        {
            if (ex is null)
            {
                return false;
            }

            return _config.OverridableExceptionsToRetryAgainstLeaderCluster.Exists(item => ex.ToString().ToLower().Contains(item[0].ToString().ToLower()));
        }
    }
}
