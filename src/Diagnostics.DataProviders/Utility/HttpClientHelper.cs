using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    internal class HttpClientHelper
    {
        /// <summary>
        /// Gets the HTTP response message as a string.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="method">The http method.</param>
        /// <param name="client">The HTTP client.</param>
        /// <param name="httpContent">Content of the HTTP request.</param>
        /// <param name="clientId">Optional parameter identifying client.</param>
        /// <param name="serializedContent">Serialized content of the HTTP request, if special serialization is needed.</param>
        /// <param name="traceId">The trace identifier.</param>
        /// <param name="numAttempts">The number of attempts.</param>
        /// <returns>
        /// The HTTP response message as a string.
        /// </returns>
        /// <remarks>
        /// We attempt up to 3 times with delay of 5 seconds and 10 seconds in between respectively, if the request cannot be sent or the response status code is 503.
        /// However, we don't want to retry in the OBO case.
        /// </remarks>
        public static async Task<Tuple<string, HttpResponseMessage>> GetResponse(
            Uri url,
            HttpMethod method,
            HttpClient client,
            object httpContent = null,
            string clientId = "",
            string serializedContent = null,
            Guid? traceId = null,
            int numAttempts = 3)
        {
            const int baseWaitTimeInSeconds = 5;
            Exception lastException = null;

            var stopWatch = Stopwatch.StartNew();
            for (int i = 1; i <= numAttempts; i++)
            {
                try
                {
                    return await GetResponseNoRetry(url, method, client, httpContent, clientId, serializedContent, traceId).ConfigureAwait(false);
                }
                catch (MetricsClientException e)
                {
                    lastException = e;

                    if (stopWatch.Elapsed >= client.Timeout ||
                        (e.ResponseStatusCode != null && e.ResponseStatusCode != HttpStatusCode.ServiceUnavailable) ||
                        i == numAttempts)
                    {
                        throw;
                    }

                    var delay = TimeSpan.FromSeconds(baseWaitTimeInSeconds * i);

                    await Task.Delay(delay).ConfigureAwait(false);
                }
            }

            throw new MetricsClientException($"Exhausted {numAttempts} attempts.", lastException);
        }

        /// <summary>
        /// Creates the HTTP client
        /// </summary>
        /// <param name="timeout">The timeout to apply to the requests.</param>
        /// <returns>
        /// An instance of <see cref="HttpClient" />
        /// </returns>
        public static HttpClient CreateHttpClient(TimeSpan timeout)
        {
            var handler = new HttpClientHandler();
            var httpClient = new HttpClient(handler, disposeHandler: true);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            httpClient.Timeout = timeout;

            return httpClient;
        }

        /// <summary>
        /// Creates the HTTP client
        /// </summary>
        /// <param name="certificate">The certificate</param>
        /// <returns>
        /// An instance of <see cref="HttpClient" />
        /// </returns>
        public static HttpClient CreateHttpClientWithAuthInfo(X509Certificate2 certificate)
        {
            var handler = new HttpClientHandler();

            if (certificate != null)
            {
                handler.ClientCertificates.Add(certificate);
                handler.ServerCertificateCustomValidationCallback = delegate { return true; };
            }

            var httpClient = new HttpClient(handler, true);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MultiDimensionalMetricsClient");
            httpClient.DefaultRequestHeaders.Add("MultiDimensionalMetricsClientVersion", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            return httpClient;
        }

        private static async Task<Tuple<string, HttpResponseMessage>> GetResponseNoRetry(
                    Uri url,
                    HttpMethod method,
                    HttpClient client,
                    object httpContent,
                    string clientId,
                    string serializedContent,
                    Guid? traceId)
        {
            traceId = traceId ?? Guid.NewGuid();

            var request = new HttpRequestMessage(method, url);
            var sourceId = Environment.MachineName;
            AddStandardHeadersToMessage(request, traceId.Value, sourceId);
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                request.Headers.Add("ClientId", clientId);
            }

            if (httpContent != null && serializedContent == null)
            {
                serializedContent = JsonConvert.SerializeObject(httpContent);
            }

            if (serializedContent != null)
            {
                request.Content = new StringContent(serializedContent, Encoding.UTF8, "application/json");
            }

            string responseString = null;
            var requestLatency = Stopwatch.StartNew();
            var stage = "SendRequest";
            var handlingServer = "Unknown";
            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request).ConfigureAwait(false);

                stage = "ReadResponse";

                IEnumerable<string> handlingServerValues;
                response.Headers.TryGetValues("__HandlingServerId__", out handlingServerValues);
                if (handlingServerValues != null)
                {
                    handlingServer = handlingServerValues.First();
                }

                requestLatency.Restart();

                if (response.Content.Headers.ContentType?.MediaType != null
                    && response.Content.Headers.ContentType.MediaType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
                {
                    responseString = "application/octet-stream";
                }
                else
                {
                    responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }

                stage = "ValidateStatus";
                response.EnsureSuccessStatusCode();

                return Tuple.Create(responseString, response);
            }
            catch(Exception e)
            {
                var message = $"Failed to get a response from the server. TraceId:{traceId.Value.ToString("B")}, Url:{request.RequestUri}, HandlingServer:{handlingServer} Stage:{stage}, "
                    + $"LatencyMs:{requestLatency.ElapsedMilliseconds}, ResponseStatus:{response?.StatusCode.ToString() ?? "<none>"}, Response:{responseString}";

                throw new MetricsClientException(message, e, traceId.Value, response?.StatusCode);
            }
            finally
            {
                requestLatency.Stop();
            }
        }

        private static void AddStandardHeadersToMessage(HttpRequestMessage message, Guid traceId, string sourceIdentity)
        {
            message.Headers.Add("TraceGuid", traceId.ToString("B"));
            message.Headers.Add("SourceIdentity", sourceIdentity);
        }
    }
}
