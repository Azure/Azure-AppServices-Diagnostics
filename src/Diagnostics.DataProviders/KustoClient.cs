using System;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;

namespace Diagnostics.DataProviders
{
    public class KustoClient : IKustoClient
    {
        private string _requestId;
        private string KustoApiQueryEndpoint;
        private static readonly string _dataSizeExceededMessage = "Query result set has exceeded the internal data size limit";
        private static readonly string _recordCountExceededMessage = "Query result set has exceeded the internal record count limit";
        private static readonly string _queryLimitDocsLink = "https://docs.microsoft.com/en-us/azure/kusto/concepts/querylimits";
       
        /// <summary>
        /// Failover Cluster Mapping.
        /// </summary>
        public ConcurrentDictionary<string, string> FailoverClusterMapping { get; set; }

        private readonly Lazy<HttpClient> _client = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        });

        private HttpClient _httpClient
        {
            get
            {
                return _client.Value;
            }
        }

        public KustoClient(KustoDataProviderConfiguration config, string requestId)
        {
            _requestId = requestId;
            KustoApiQueryEndpoint = config.KustoApiEndpoint + ":443/v1/rest/query";
            FailoverClusterMapping = config.FailoverClusterNameCollection;
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, string cluster, string database, string requestId = null, string operationName = null)
        {

            return await ExecuteQueryAsync(query, cluster, database, DataProviderConstants.DefaultTimeoutInSeconds, requestId, operationName);
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, string cluster, string database, int timeoutSeconds, string requestId = null, string operationName = null)
        {
            var timeTakenStopWatch = new Stopwatch();
            var authorizationToken = await KustoTokenService.Instance.GetAuthorizationTokenAsync();
            var kustoClientId = $"Diagnostics.{operationName ?? "Query"};{_requestId}##{0}";
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var request = new HttpRequestMessage(HttpMethod.Post, KustoApiQueryEndpoint.Replace("{cluster}", cluster));
            request.Headers.Add("Authorization", authorizationToken);
            request.Headers.Add(HeaderConstants.ClientRequestIdHeader, requestId ?? Guid.NewGuid().ToString());
            request.Headers.UserAgent.ParseAdd("appservice-diagnostics");
            var requestPayload = new
            {
                db = database,
                csl = query,
                properties = new
                {
                    ClientRequestId = kustoClientId,
                    Options = new
                    {
                        servertimeout = new TimeSpan(0, 1, 0)
                    }
                }
            };
            request.Content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
            DataTableResponseObjectCollection dataSet = null;
            string responseContent = string.Empty;
            try
            {
                timeTakenStopWatch.Start();

                var responseMsg = await _httpClient.SendAsync(request, tokenSource.Token);
                responseContent = await responseMsg.Content.ReadAsStringAsync();

                if (!responseMsg.IsSuccessStatusCode)
                {
                    timeTakenStopWatch.Stop();
                    LogKustoQuery(query, cluster, operationName, timeTakenStopWatch, kustoClientId, new Exception($"Kusto call ended with a non success status code : {responseMsg.StatusCode.ToString()}"), dataSet, responseContent);
                    throw new Exception(responseContent);
                }
                else
                {
                    dataSet = ProcessKustoResponse(responseContent);
                }
            }            
            catch (Exception ex)
            {
                timeTakenStopWatch.Stop();
                LogKustoQuery(query, cluster, operationName, timeTakenStopWatch, kustoClientId, ex, dataSet, responseContent);

                throw;
            }
            finally
            {
                timeTakenStopWatch.Stop();
            }

            LogKustoQuery(query, cluster, operationName, timeTakenStopWatch, kustoClientId, null, dataSet);

            return dataSet?.Tables == null ? new DataTable() : dataSet.Tables.FirstOrDefault().ToDataTable();
        }

        private DataTableResponseObjectCollection ProcessKustoResponse(string responseContent)
        {
            if (responseContent.Contains(_dataSizeExceededMessage))
            {
                throw new Exception($"Kusto query response exceeded the Kusto data size limit: {_queryLimitDocsLink}");
            }
            else if (responseContent.Contains(_recordCountExceededMessage))
            {
                throw new Exception($"Kusto query response exceeded the Kusto record count limit: {_queryLimitDocsLink}");
            }

            return JsonConvert.DeserializeObject<DataTableResponseObjectCollection>(responseContent);
        }

        private void LogKustoQuery(string query, string cluster, string operationName, Stopwatch timeTakenStopWatch, string kustoClientId, Exception kustoApiException, DataTableResponseObjectCollection dataSet, string kustoResponse = "")
        {
            var status = kustoApiException == null ? "Success" : "Failed";

            kustoResponse = (kustoResponse != "") ? $" {Environment.NewLine} KustoResponseBody : {kustoResponse} " : string.Empty ;

            object stats = null;
            if (dataSet != null && dataSet.Tables != null && dataSet.Tables.Count() >= 4)
            {
                var statisticsTable = dataSet.Tables.ToArray()[dataSet.Tables.Count() - 2];
                if (statisticsTable.Rows.GetLength(0) >= 2 && statisticsTable.Rows.GetLength(1) >= 5)
                {
                    stats = statisticsTable.Rows[1, 4];
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

        public async Task<KustoQuery> GetKustoQueryAsync(string query, string cluster, string database)
        {
            return await GetKustoQueryAsync(query, cluster, database, null);
        }

        public async Task<KustoQuery> GetKustoQueryAsync(string query, string cluster, string database, string operationName)
        {
            try
            {
                if(FailoverClusterMapping.ContainsKey(cluster))
                {
                    cluster = FailoverClusterMapping[cluster];
                }
                var encodedQuery = await EncodeQueryAsBase64UrlAsync(query);
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

        // From Kusto.Data.Common.CslCommandGenerator.EncodeQueryAsBase64Url
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
    }
}
