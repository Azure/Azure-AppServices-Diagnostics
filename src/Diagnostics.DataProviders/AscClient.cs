using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;
using Newtonsoft.Json;
using Diagnostics.ModelsAndUtils.Models.ChangeAnalysis;
using Diagnostics.DataProviders.TokenService;
using System.Collections.Generic;
using System.Net;
using System.Web;
using Diagnostics.DataProviders.DataProviderConfigurations;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Client to call into Azure Support Center.
    /// </summary>
    public class AscClient : IAscClient
    {

        /// <summary>
        /// Azure Support Center base uri, initialized from config.
        /// </summary>
        private string baseUri;

        /// <summary>
        /// Azure Support Center api uri, initialized from config.
        /// </summary>
        private string apiUri;

        /// <summary>
        /// Azure Support Center api version, initialized from config.
        /// </summary>
        private string apiVersion;

        /// <summary>
        /// User-agent to pass to Azure Support Center, initialized from config.
        /// </summary>
        private string userAgent;

        private string requestId;

        private readonly Lazy<HttpClient> client = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
       );

        /// <summary>
        /// Initializes a new instance of the <see cref="AscClient"/> class.
        /// <param name="config">Config for Asc Data Provider.</param>
        /// <param name="requestId">AppLens Request Id, used for logging.</param>
        /// </summary>
        public AscClient(AscDataProviderConfiguration config, string requestId)
        {
            baseUri = config.BaseUri;
            apiUri = config.ApiUri;
            apiVersion = config.ApiVersion;
            userAgent = config.UserAgent;
        }

        private HttpClient httpClient
        {
            get
            {
                return client.Value;
            }
        }

        /// <inheritdoc/>
        public async Task<T> MakeHttpPostRequest<T>(string jsonPostBody, string apiVersion, CancellationToken cancellationToken = default(CancellationToken))
        {
            string authToken = await AscTokenService.Instance.GetAuthorizationTokenAsync();
            try
            {
                if (string.IsNullOrEmpty(apiVersion))
                {
                    apiVersion = this.apiVersion;
                }

                string requestUri = baseUri + apiUri + "?" + apiVersion.Replace("?", string.Empty);

                httpClient.DefaultRequestHeaders.UserAgent.Clear();
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue(userAgent)));

                using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri))
                {
                    requestMessage.Headers.Add("Authorization", authToken);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    requestMessage.Content = new StringContent(jsonPostBody, Encoding.UTF8, "application/json");

                    var response = await httpClient.SendAsync(requestMessage, cancellationToken);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        if (typeof(T).Equals(typeof(string)))
                        {
                            return CastTo<T>(responseContent);
                        }
                        else
                        {
                            T value = JsonConvert.DeserializeObject<T>(responseContent);
                            return value;
                        }
                    }
                    else
                    {
                        string responseLogBody = await GetRequestDetailsForLogging(response);
                        throw new Exception(string.Format("HTTP POST request to ASC failed. AppLens request Id : {0} ==> Details : {1}", this.requestId, responseLogBody));
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                if (cancellationToken != default(CancellationToken))
                {
                    throw new Exception(string.Format("The HTTP POST request to ASC with body {0} was cancelled as per the supplied cancellation token. AppLens request Id : {1}", jsonPostBody, requestId), ex);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<T> MakeHttpGetRequest<T>(string queryString, string apiVersion, CancellationToken cancellationToken = default(CancellationToken))
        {
            string requestUri = string.Empty;

            if (string.IsNullOrEmpty(apiVersion))
            {
                apiVersion = this.apiVersion;
            }

            string authToken = await AscTokenService.Instance.GetAuthorizationTokenAsync();
            try
            {
                if (string.IsNullOrEmpty(queryString))
                {
                    requestUri = baseUri + apiUri + "?" + apiVersion.Replace("?", string.Empty);
                }
                else
                {
                    requestUri = baseUri + apiUri + "?" + queryString.Replace("?", string.Empty) + " &" + apiVersion.Replace("?", string.Empty);
                }

                httpClient.DefaultRequestHeaders.UserAgent.Clear();
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue(userAgent)));

                using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri))
                {
                    requestMessage.Headers.Add("Authorization", authToken);

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                    var response = await httpClient.SendAsync(requestMessage, cancellationToken);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        if (typeof(T).Equals(typeof(string)))
                        {
                            return CastTo<T>(responseContent);
                        }
                        else
                        {
                            T value = JsonConvert.DeserializeObject<T>(responseContent);
                            return value;
                        }
                    }
                    else
                    {
                        string responseLogBody = await GetRequestDetailsForLogging(response);
                        throw new Exception(string.Format("HTTP GET request to ASC failed. AppLens request Id : {0} ==> Details : {1}", requestId, responseLogBody));
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                if (cancellationToken != default(CancellationToken))
                {
                    throw new Exception(string.Format("The HTTP GET request to ASC with query string {0} was cancelled as per the supplied cancellation token. AppLens request Id : {1}", queryString, requestId), ex);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<T> GetInsightFromBlob<T>(string blobUri, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                httpClient.DefaultRequestHeaders.Remove("Authorization");
                httpClient.DefaultRequestHeaders.UserAgent.Clear();
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue(userAgent)));
                var response = await httpClient.GetAsync(blobUri);
                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    if (typeof(T).Equals(typeof(string)))
                    {
                        return CastTo<T>(responseContent);
                    }
                    else
                    {
                        T value = JsonConvert.DeserializeObject<T>(responseContent);
                        return value;
                    }
                }
                else
                {
                    string responseLogBody = await GetRequestDetailsForLogging(response);
                    throw new Exception(string.Format("Request to fetch content from blob for ASC failed. AppLens request Id : {0} ==> Details : {1}", requestId, responseLogBody));
                }
            }
            catch (TaskCanceledException ex)
            {
                if (cancellationToken != default(CancellationToken))
                {
                    throw new Exception(string.Format("The request to blob {0} for ASC was cancelled as per the supplied cancellation token. AppLens request Id : {1}", blobUri, requestId), ex);
                }
                else
                {
                    throw;
                }
            }
        }

        private static T CastTo<T>(object obj)
        {
            try
            {
                return (T)obj;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to cast object from {0} to {1}", obj.GetType().ToString(), typeof(T).ToString()), ex);
            }
        }

        /// <summary>
        /// Generates a JSON string containing request and response headers along with the response content. Is used for logging purposes.
        /// </summary>
        /// <param name="response">HTTP Response message.</param>
        /// <returns>JSON formatted header and body content.</returns>
        private async Task<string> GetRequestDetailsForLogging(HttpResponseMessage response)
        {
            HTTPResponseLog logMessage = new HTTPResponseLog();
            if (await logMessage.Initialize(response))
            {
                return JsonConvert.SerializeObject(logMessage);
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Class to represent HTTP Request / Response. Used for logging purposes.
        /// </summary>
        public class HTTPResponseLog
        {
            /// <summary>
            /// URI for the HTTP request.
            /// </summary>
            public string requestUri;

            /// <summary>
            /// HTTP method. GET / POST etc.
            /// </summary>
            public HttpMethod method;

            /// <summary>
            /// Request header Collection.
            /// </summary>
            public HttpRequestHeaders requestHeaders;

            /// <summary>
            /// Request body. Empty in case of a GET request.
            /// </summary>
            public string requestBody;

            /// <summary>
            /// Response status code.
            /// </summary>
            public HttpStatusCode responseStatusCode;

            /// <summary>
            /// Response header Collection.
            /// </summary>
            public HttpResponseHeaders responseHeaders;

            /// <summary>
            /// Response body.
            /// </summary>
            public string responseBody;

            /// <summary>
            /// Initializes the <see cref="HTTPResponseLog"/> class.
            /// </summary>
            /// <param name="response">HTTP Response object that you need to create a log for.</param>
            /// <returns>True if initialized and false otherwise.</returns>
            public async Task<bool> Initialize(HttpResponseMessage response)
            {
                try
                {
                    requestUri = response.RequestMessage.RequestUri.ToString();
                    method = response.RequestMessage.Method;
                    requestHeaders = response.RequestMessage.Headers;
                    responseStatusCode = response.StatusCode;
                    responseHeaders = response.Headers;
                    requestBody = response.RequestMessage.Method.Equals(HttpMethod.Post) ? await response.RequestMessage.Content.ReadAsStringAsync() : string.Empty;
                    responseBody = await response.Content.ReadAsStringAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    // Intentionally swallowing any exception here since this is only for logging purposes.
                    return false;
                }
            }
        }
    }
}
