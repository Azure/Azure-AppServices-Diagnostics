using System;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;

namespace Diagnostics.DataProviders
{
    public class AppInsightsClient : IAppInsightsClient
    {
        private AppInsightsDataProviderConfiguration _configuration;

        /// <summary>
        /// Application ID
        /// </summary>
        private string _appId { get; set; }

        /// <summary>
        /// API Key
        /// </summary>
        private string _apiKey { get; set; }

        /// <summary>
        /// Applicaion Insights Endpoint
        /// </summary>
        private const string BaseURL = "https://api.applicationinsights.io/v1/apps/{0}/query";

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

        public AppInsightsClient(AppInsightsDataProviderConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void SetAppInsightsKey(string appId, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(appId))
            {
                throw new ArgumentNullException("appId");
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException("apiKey");
            }
            _appId = appId;
            _apiKey = apiKey;
        }

        public async Task<DataTable> ExecuteQueryAsync(string queryString)
        {
            if (string.IsNullOrWhiteSpace(_appId))
            {
                throw new ArgumentNullException("_appId");
            }
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new ArgumentNullException("_apiKey");
            }
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(BaseURL, _appId));
            request.Headers.Add("x-api-key", _apiKey);

            object requestPayload = new
            {
                query = queryString
            };

            request.Content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
            CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(DataProviderConstants.DefaultTimeoutInSeconds));
            HttpResponseMessage responseMsg = await _httpClient.SendAsync(request, tokenSource.Token);
            string content = await responseMsg.Content.ReadAsStringAsync();

            if (!responseMsg.IsSuccessStatusCode)
            {
                throw new Exception(content);
            }

            AppInsightsDataTableResponseObjectCollection dataSet = JsonConvert.DeserializeObject<AppInsightsDataTableResponseObjectCollection>(content);

            if (dataSet == null || dataSet.Tables == null)
            {
                return new DataTable();
            }
            else
            {
                return dataSet.Tables.FirstOrDefault().ToAppInsightsDataTable();
            }
        }
    }
}
