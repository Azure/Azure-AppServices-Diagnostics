using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
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
        private KustoDataProviderConfiguration _configuration;

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

        public KustoClient(KustoDataProviderConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, string stampName, string requestId = null, string operationName = null)
        {
            string kustoClusterName = GetClusterNameFromStamp(stampName);

            string authorizationToken = await KustoTokenService.Instance.GetAuthorizationTokenAsync();

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(KustoApiEndpoint, kustoClusterName));
            request.Headers.Add("Authorization", authorizationToken);
            request.Headers.Add("x-ms-client-request-id", requestId ?? Guid.NewGuid().ToString());

            object requestPayload = new
            {
                db = _configuration.DBName,
                csl = query
            };

            request.Content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

            CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(DataProviderConstants.DefaultTimeoutInSeconds));
            HttpResponseMessage responseMsg = await _httpClient.SendAsync(request, tokenSource.Token);
            string content = await responseMsg.Content.ReadAsStringAsync();

            if (!responseMsg.IsSuccessStatusCode)
            {
                throw new Exception(content);
            }

            DataTableResponseObjectCollection dataSet = JsonConvert.DeserializeObject<DataTableResponseObjectCollection>(content);

            if (dataSet == null || dataSet.Tables == null)
            {
                return new DataTable();
            }
            else
            {
                return dataSet.Tables.FirstOrDefault().ToDataTable();
            }
        }

        public async Task<string> GetKustoQueryUriAsync(string stampName, string query)
        {
            string kustoClusterName = null;
            try
            {
                kustoClusterName = GetClusterNameFromStamp(stampName);
                string encodedQuery = await EncodeQueryAsBase64UrlAsync(query);
                var url = string.Format("https://web.kusto.windows.net/clusters/{0}.kusto.windows.net/databases/{1}?q={2}", kustoClusterName, _configuration.DBName, encodedQuery);
                return url;
            }
            catch (Exception ex)
            {
                string message = string.Format("stamp : {0}, kustoClusterName : {1}, Exception : {2}",
                    stampName ?? "null",
                    kustoClusterName ?? "null",
                    ex.ToString());
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
                throw new ArgumentNullException("stampName");
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
