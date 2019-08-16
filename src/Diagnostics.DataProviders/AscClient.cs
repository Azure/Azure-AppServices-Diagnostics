using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.DataProviders.TokenService;
using Diagnostics.Logger;
using Newtonsoft.Json;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Client to call into Azure Support Center.
    /// </summary>
    public class AscClient : IAscClient
    {
        /// <summary>
        /// User-agent to pass to Azure Support Center, initialized from config.
        /// </summary>
        public static string UserAgent
        {
            get { return AscClient.userAgent; }
        }

        private static string userAgent = string.Empty;

        /// <summary>
        /// Request id for the current Applens request, used for logging.
        /// </summary>
        private readonly string requestId;

        /// <summary>
        /// Logging helper.
        /// </summary>
        private readonly DiagnosticsETWProvider logger;

        private readonly Lazy<HttpClient> client = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd(AscClient.UserAgent);
            return client;
        });

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
        /// Initializes a new instance of the <see cref="AscClient"/> class.
        /// <param name="config">Config for Asc Data Provider.</param>
        /// <param name="appLensRequestId">AppLens Request Id, used for logging.</param>
        /// </summary>
        public AscClient(AscDataProviderConfiguration config, string appLensRequestId)
        {
            baseUri = config.BaseUri;
            apiUri = config.ApiUri;
            apiVersion = config.ApiVersion;
            AscClient.userAgent = config.UserAgent;
            logger = DiagnosticsETWProvider.Instance;
            requestId = appLensRequestId;
        }

        private HttpClient httpClient
        {
            get
            {
                return client.Value;
            }
        }        

        /// <inheritdoc/>
        public async Task<T> MakeHttpPostRequest<T>(string jsonPostBody, string apiUri, string apiVersion, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiUri))
                {
                    apiUri = this.apiUri;
                }

                if (string.IsNullOrWhiteSpace(apiVersion))
                {
                    apiVersion = this.apiVersion;
                }

                string requestUri = baseUri + apiUri + "?api-version=" + apiVersion.Replace("?", string.Empty).Replace("api-version=", string.Empty);

                using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri))
                {
                    requestMessage.Headers.Add("Authorization", await AscTokenService.Instance.GetAuthorizationTokenAsync());
                    requestMessage.Content = new StringContent(jsonPostBody, Encoding.UTF8, "application/json");
                    return await GetAscResponse<T>(requestMessage, false, cancellationToken);
                }
            }
            catch (TaskCanceledException ex)
            {
                if (cancellationToken != default(CancellationToken))
                {
                    throw new TaskCanceledException(string.Format("The HTTP POST request to ASC with body {0} was cancelled as per the supplied cancellation token. AppLens request Id : {1}", jsonPostBody, requestId), ex);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<T> MakeHttpGetRequest<T>(string queryString, string apiUri, string apiVersion, CancellationToken cancellationToken = default(CancellationToken))
        {
            string requestUri = string.Empty;

            if (string.IsNullOrWhiteSpace(apiUri))
            {
                apiUri = this.apiUri;
            }

            if (string.IsNullOrWhiteSpace(apiVersion))
            {
                apiVersion = this.apiVersion;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(queryString))
                {
                    requestUri = baseUri + apiUri + "?api-version=" + apiVersion.Replace("?", string.Empty).Replace("api-version=", string.Empty);
                }
                else
                {
                    requestUri = baseUri + apiUri + "?" + queryString.Replace("?", string.Empty) + "&api-version=" + apiVersion.Replace("?", string.Empty).Replace("api-version=", string.Empty);
                }

                using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri))
                {
                    requestMessage.Headers.Add("Authorization", await AscTokenService.Instance.GetAuthorizationTokenAsync());
                    return await GetAscResponse<T>(requestMessage, false, cancellationToken);
                }
            }
            catch (TaskCanceledException ex)
            {
                if (cancellationToken != default(CancellationToken))
                {
                    throw new TaskCanceledException(string.Format("The HTTP GET request to ASC with query string {0} was cancelled as per the supplied cancellation token. AppLens request Id : {1}", queryString, requestId), ex);
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
                Uri blobUriObj;
                if (string.IsNullOrWhiteSpace(blobUri))
                {
                    throw new ArgumentNullException($"Empty blob uri {blobUri}.");
                }

                if (!Uri.TryCreate(blobUri, UriKind.RelativeOrAbsolute, out blobUriObj))
                {
                    throw new ArgumentException($"Invalid blob uri {blobUri}.");
                }

                using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, blobUriObj))
                {
                    return await GetAscResponse<T>(requestMessage, true, cancellationToken);
                }
            }
            catch (TaskCanceledException ex)
            {
                if (cancellationToken != default(CancellationToken))
                {
                    throw new TaskCanceledException(string.Format("The request to blob {0} for ASC was cancelled as per the supplied cancellation token. AppLens request Id : {1}", blobUri, requestId), ex);
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
                throw new InvalidCastException(string.Format("Failed to cast object from {0} to {1}", obj.GetType().ToString(), typeof(T).ToString()), ex);
            }
        }

        private async Task<T> GetAscResponse<T>(HttpRequestMessage requestMessage, bool isBlobRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = await SendAscRequestAsync(requestMessage, isBlobRequest, cancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                if (typeof(T).Equals(typeof(string)))
                {
                    return CastTo<T>(responseContent);
                }
                else
                {
                    T value;
                    try
                    {
                        value = JsonConvert.DeserializeObject<T>(responseContent);
                    }
                    catch (JsonSerializationException serializeException)
                    {
                        throw new JsonSerializationException($" Failed to serialize ASC response to type {typeof(T).ToString()} : Response from ASC ==> {responseContent}", serializeException);
                    }

                    return value;
                }
            }
            else
            {
                string responseLogBody = await GetRequestDetailsForLogging(response);
                throw new HttpRequestException(string.Format("Request to fetch content from blob for ASC failed. AppLens request Id : {0} ==> Details : {1}", requestId, responseLogBody));
            }
        }

        private async Task<HttpResponseMessage> SendAscRequestAsync(HttpRequestMessage request, bool isBlobRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            logger.LogDataProviderMessage(requestId, "AscDataProvider", $"url:{request.RequestUri}, statusCode:{response.StatusCode}");
            return response;
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
                    response.RequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "XXXX");
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
