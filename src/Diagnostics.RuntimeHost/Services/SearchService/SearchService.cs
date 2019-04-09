using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Diagnostics.RuntimeHost.Services
{
    public interface ISearchService : IDisposable
    {
        Task<HttpResponseMessage> SearchDetectors(string query, Dictionary<string, string> parameters);

        Task<HttpResponseMessage> SearchUtterances(string query, string productId);
    }

    public class SearchService : ISearchService
    {
        private string QueryDetectorsUrl;
        private string QueryUtterancesUrl;
        private string RefreshModelUrl;
        private string FreeModelUrl;
        private HttpClient _httpClient;
        
        public SearchService()
        {
            QueryDetectorsUrl = "http://localhost:8010/queryDetectors";
            QueryUtterancesUrl = "http://localhost:8010/queryUtterances";
            InitializeHttpClient();
        }

        public Task<HttpResponseMessage> Get(HttpRequestMessage request)
        {
            return _httpClient.SendAsync(request);
        }

        public Task<HttpResponseMessage> SearchDetectors(string query, Dictionary<string, string> parameters)
        {
            parameters.Add("text", query);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, QueryDetectorsUrl);
            request.Content = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json");
            return Get(request);
        }

        public Task<HttpResponseMessage> SearchUtterances(string query, string productId)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, AppendQueryStringParams(QueryUtterancesUrl, query, productId));
            return Get(request);
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
                Timeout = TimeSpan.FromSeconds(30)
            };

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private string AppendQueryStringParams(string url, string query, string productid)
        {
            var uriBuilder = new UriBuilder(url);
            var queryParams = HttpUtility.ParseQueryString(uriBuilder.Query);
            queryParams.Add("text", query);
            queryParams.Add("productid", productid);
            uriBuilder.Query = queryParams.ToString();
            return uriBuilder.ToString();
        }
    }
}
