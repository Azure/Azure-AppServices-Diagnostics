using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public class KustoClient: IKustoClient
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
            string appserviceRegion = ParseRegionFromStamp(stampName);

            string kustoClusterName;
            if (!_configuration.RegionSpecificClusterNameCollection.TryGetValue(appserviceRegion.ToLower(), out kustoClusterName))
            {
                // try to use default cluster name.
                if (!_configuration.RegionSpecificClusterNameCollection.TryGetValue("*", out kustoClusterName))
                {
                    throw new KeyNotFoundException(String.Format("Kusto Cluster Name not found for Region : {0}", appserviceRegion.ToLower()));
                }
            }

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

            DataTableResponseObjectCollection dataSet = JsonConvert.DeserializeObject<DataTableResponseObjectCollection>(content);

            if (dataSet.Tables == null)
            {
                return new DataTable();
            }
            else
            {
                return dataSet.Tables.FirstOrDefault().ToDataTable();
            }
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
