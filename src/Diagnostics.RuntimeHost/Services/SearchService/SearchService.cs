using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Diagnostics.RuntimeHost.Services
{
    public interface ISearchService : IDisposable
    {
        bool IsEnabled();
        Task<HttpResponseMessage> SearchDetectors(string requestId, string query, Dictionary<string, string> parameters);

        Task<HttpResponseMessage> SearchUtterances(string requestId, string query, string[] detectorUtterances, Dictionary<string, string> parameters);

        Task<HttpResponseMessage> TriggerTraining(string requestId, string trainingConfig, Dictionary<string, string> parameters);

        Task<HttpResponseMessage> TriggerModelRefresh(string requestId, Dictionary<string, string> parameters);
    }

    public class SearchService : ISearchService
    {
        private string QueryDetectorsUrl;
        private string QueryUtterancesUrl;
        private string RefreshModelUrl;
        private string FreeModelUrl;
        private string TriggerTrainingUrl;
        private string TriggerModelRefreshUrl;
        private HttpClient _httpClient;
        private IConfiguration _config;
        private IHostingEnvironment _env;
        private bool isEnabled;

        public SearchService(IHostingEnvironment environment, IConfiguration configuration)
        {
            _config = configuration;
            _env = environment;
            QueryDetectorsUrl = UriElements.SearchAPI + "/queryDetectors";
            QueryUtterancesUrl = UriElements.SearchAPI + "/queryUtterances";
            TriggerTrainingUrl = UriElements.TrainingAPI + "/triggerTraining";
            TriggerModelRefreshUrl = UriElements.SearchAPI + "/refreshModel";
            isEnabled = false;
            LoadConfigurations();
            if (isEnabled)
            {
                InitializeHttpClient();
            }
        }

        public bool IsEnabled()
        {
            return isEnabled;
        }

        public void LoadConfigurations()
        {
            if (_env.IsProduction())
            {
                isEnabled = Convert.ToBoolean(Registry.GetValue(RegistryConstants.SearchAPIRegistryPath, RegistryConstants.SearchAPIEnabledKey, string.Empty));
            }
            else
            {
                isEnabled = Convert.ToBoolean(_config[$"SearchAPI:{RegistryConstants.SearchAPIEnabledKey}"]);
            }
        }

        public async Task<HttpResponseMessage> Get(HttpRequestMessage request)
        {
            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> SearchDetectors(string requestId, string query, Dictionary<string, string> parameters)
        {
            if (isEnabled)
            {
                parameters.Add("text", query);
                parameters.Add("requestId", requestId ?? string.Empty);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, QueryDetectorsUrl);
                request.Content = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json");
                return await Get(request);
            }
            return null;
        }

        public async Task<HttpResponseMessage> SearchUtterances(string requestId, string query, string[] detectorUtterances, Dictionary<string, string> parameters)
        {
            if (isEnabled)
            {
                parameters.Add("detector_description", query);
                parameters.Add("detector_utterances", JsonConvert.SerializeObject(detectorUtterances));
                parameters.Add("requestId", requestId);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, QueryUtterancesUrl);
                request.Content = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json");
                return await Get(request);
            }
            return null;
        }

        public Task<HttpResponseMessage> TriggerTraining(string requestId, string trainingConfig, Dictionary<string, string> parameters)
        {
            if (isEnabled)
            {
                parameters.Add("trainingConfig", trainingConfig);
                parameters.Add("requestId", requestId);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, TriggerTrainingUrl);
                request.Content = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json");
                return Get(request);
            }
            return null;
        }

        public Task<HttpResponseMessage> TriggerModelRefresh(string requestId, Dictionary<string, string> parameters)
        {
            if (isEnabled)
            {
                parameters.Add("requestId", requestId);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, AppendQueryStringParams(TriggerModelRefreshUrl, parameters));
                return Get(request);
            }
            return null;
        }

        public void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }
        }

        private void InitializeHttpClient()
        {
            _httpClient = new HttpClient
            {
                MaxResponseContentBufferSize = Int32.MaxValue,
                Timeout = TimeSpan.FromSeconds(90)
            };

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private string AppendQueryStringParams(string url, Dictionary<string, string> additionalQueryParams)
        {
            var uriBuilder = new UriBuilder(url);
            var queryParams = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (string key in additionalQueryParams.Keys)
            {
                queryParams.Add(key, additionalQueryParams.GetValueOrDefault(key));
            }

            uriBuilder.Query = queryParams.ToString();
            return uriBuilder.ToString();
        }
    }
}
