using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

namespace Diagnostics.DataProviders
{
    public class KustoClient : IKustoClient
    {
        private string _requestId;

        /// <summary>
        /// Kusto Endpoint
        /// </summary>
        private const string KustoApiEndpoint = "https://{0}.kusto.windows.net:443/v1/rest/query";

        private readonly Lazy<HttpClient> _client = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
        );

        private HttpClient _httpClient
        {
            get
            {
                return _client.Value;
            }
        }

        public KustoClient(string requestId)
        {
            _requestId = requestId;
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, string cluster, string database, string requestId = null, string operationName = null)
        {
            var timeTakenStopWatch = new Stopwatch();

            string authorizationToken = await KustoTokenService.Instance.GetAuthorizationTokenAsync();

            var kustoClientId = $"Diagnostics.{operationName ?? "Query"};{_requestId}##{0}";

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(KustoApiEndpoint, cluster));
            request.Headers.Add("Authorization", authorizationToken);
            request.Headers.Add("x-ms-client-request-id", requestId ?? Guid.NewGuid().ToString());
            request.Headers.UserAgent.ParseAdd("appservice-diagnostics"); 

            object requestPayload = new
            {
                db = database,
                csl = query
            };

            request.Content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

            CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(DataProviderConstants.DefaultTimeoutInSeconds));

            timeTakenStopWatch.Start();

            Exception kustoApiException = null;
            string responseContent = null;
            DataTableResponseObjectCollection dataSet = null;
            try
            {
                HttpResponseMessage responseMsg = await _httpClient.SendAsync(request, tokenSource.Token);
                responseContent = await responseMsg.Content.ReadAsStringAsync();

                if (!responseMsg.IsSuccessStatusCode)
                {
                    throw new Exception(responseContent);
                }
                else
                {
                    dataSet = JsonConvert.DeserializeObject<DataTableResponseObjectCollection>(responseContent);
                }
            }
            catch(Exception ex)
            {
                kustoApiException = ex;
                throw;
            }
            finally
            {
                timeTakenStopWatch.Stop();
                var status = kustoApiException == null ? "Success" : "Failed";

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
                   kustoApiException != null ? kustoApiException.ToString() : string.Empty);
            }

            

            if (dataSet == null || dataSet.Tables == null)
            {
                return new DataTable();
            }
            else
            {
                return dataSet.Tables.FirstOrDefault().ToDataTable();
            }
        }

        public async Task<KustoQuery> GetKustoQueryAsync(string query, string cluster, string database)
        {
            try
            {
                string encodedQuery = await EncodeQueryAsBase64UrlAsync(query);
                
                KustoQuery kustoQuery = new KustoQuery
                {
                    Text = query,
                    Url = string.Format("https://web.kusto.windows.net/clusters/{0}.kusto.windows.net/databases/{1}?q={2}", cluster, database, encodedQuery),
                    KustoDesktopUrl = $"https://{cluster}.kusto.windows.net:443/{database}?query={encodedQuery}&web=0"
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
            byte[] bytes = Encoding.UTF8.GetBytes(query);
            string result;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
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
