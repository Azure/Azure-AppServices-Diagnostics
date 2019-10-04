using System;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Kusto.Data;
using Kusto.Data.Net.Client;
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

namespace Diagnostics.DataProviders
{
    class KustoSDKClient : IKustoClient
    {
        private string _requestId;
        private string _kustoApiQueryEndpoint;
        private string _appKey;
        private string _clientId;
        private string _aadAuthority;
        private static ConcurrentDictionary<Tuple<string, string>, ICslQueryProvider> QueryProviderMapping;

        public KustoSDKClient(KustoDataProviderConfiguration config, string requestId)
        {
            if (QueryProviderMapping == null)
            {
                QueryProviderMapping = new ConcurrentDictionary<Tuple<string, string>, ICslQueryProvider>();
            }
            _requestId = requestId;
            _kustoApiQueryEndpoint = config.KustoApiEndpoint + ":443";
            _appKey = config.AppKey;
            _clientId = config.ClientId;
            _aadAuthority = config.AADAuthority;
        }

        private ICslQueryProvider client(string cluster, string database)
        {
            var key = Tuple.Create(cluster, database);
            if (!QueryProviderMapping.ContainsKey(key))
            {
                KustoConnectionStringBuilder connectionStringBuilder = new KustoConnectionStringBuilder(_kustoApiQueryEndpoint.Replace("{cluster}", cluster), database);
                connectionStringBuilder.FederatedSecurity = true;
                connectionStringBuilder.ApplicationClientId = _clientId;
                connectionStringBuilder.ApplicationKey = _appKey;
                connectionStringBuilder.Authority = _aadAuthority;
                var queryProvider = Kusto.Data.Net.Client.KustoClientFactory.CreateCslQueryProvider(connectionStringBuilder);
                if (!QueryProviderMapping.TryAdd(key, queryProvider))
                {
                    queryProvider.Dispose();
                }
            }

            return QueryProviderMapping[key];
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, string cluster, string database, int timeoutSeconds, string requestId = null, string operationName = null)
        {
            var timeTakenStopWatch = new Stopwatch();
            DataSet dataSet = null;
            ClientRequestProperties clientRequestProperties = new ClientRequestProperties();
            var kustoClientId = $"Diagnostics.{operationName ?? "Query"};{_requestId}##{0}_{(new Guid()).ToString()}";
            clientRequestProperties.ClientRequestId = kustoClientId;
            clientRequestProperties.SetOption("servertimeout", new TimeSpan(0,0,timeoutSeconds));

            try
            {
                timeTakenStopWatch.Start();
                var kustoClient = client(cluster, database);
                var result = await kustoClient.ExecuteQueryAsync(database, query, clientRequestProperties);
                dataSet = result.ToDataSet();
            }
            catch (Exception ex)
            {
                timeTakenStopWatch.Stop();
                LogKustoQuery(query, cluster, operationName, timeTakenStopWatch, kustoClientId, ex, dataSet);

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

        public async Task<DataTable> ExecuteQueryAsync(string query, string cluster, string database, string requestId = null, string operationName = null)
        {
            return await ExecuteQueryAsync(query, cluster, database, DataProviderConstants.DefaultTimeoutInSeconds, requestId, operationName);
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

            kustoResponse = (kustoResponse != "") ? $" {Environment.NewLine} KustoResponseBody : {kustoResponse} " : string.Empty;

            object stats = null;
            if (dataSet != null && dataSet.Tables != null && dataSet.Tables.Count >= 4)
            {
                var statisticsTable = dataSet.Tables[dataSet.Tables.Count - 2].ToDataTableResponseObject();
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
    }
}
