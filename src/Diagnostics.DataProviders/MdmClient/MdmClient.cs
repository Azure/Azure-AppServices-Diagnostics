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
using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.DataProviders.Utility;
using Diagnostics.Logger;
using Newtonsoft.Json;
using MetricsClient = Microsoft.Cloud.Metrics.Client;
using Serialization = Microsoft.Online.Metrics.Serialization.Configuration;
using Microsoft.Cloud.Metrics.Client.Metrics;
using Microsoft.Cloud.Metrics.Client.Query;
using Microsoft.Cloud.Metrics.Client;

namespace Diagnostics.DataProviders
{
    public class MdmClient : IMdmClient
    {
        /// <summary>
        /// Gets the request id.
        /// </summary>
        public string RequestId { get; private set; }

        /// <summary>
        /// Gets the http client.
        /// </summary>
        public static HttpClient HttpClient { get; private set; }

        public static MetricReader MetricReader { get; private set; }

        /// <summary>
        /// Gets the endpoint.
        /// </summary>
        public Uri Endpoint { get; private set; }

        private readonly string ClientId = "ClientAPI";

        /// <summary>
        /// Initializes a new instance of the <see cref="MdmClient" /> class.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        public MdmClient(string endpoint, X509Certificate2 certificate, string requestId)
        {
            try
            {
                Endpoint = new Uri(endpoint);
                HttpClient = CreateHttpClient(certificate);
                RequestId = requestId;
                MetricReader = CreateMetricReader(certificate);
            }
            catch (Exception ex)
            {
                // Log failure
                DiagnosticsETWProvider.Instance.LogDataProviderException(
                    requestId,
                    "Initialize MDM data provider",
                    DateTime.UtcNow.ToString("HH:mm:ss.fff"),
                    DateTime.UtcNow.ToString("HH:mm:ss.fff"),
                    0,
                    ex.GetType().ToString(),
                    ex.ToString());
            }
        }

        private MetricReader CreateMetricReader(X509Certificate2 certificate)
        {
            var connectionInfo = new ConnectionInfo(Endpoint, certificate);
            return new MetricReader(connectionInfo);
        }

        /// <summary>
        /// Gets the list of namespaces for the monitoringAccount.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <returns>The list of namespaces for the monitoringAccount.</returns>
        public async Task<IEnumerable<string>> GetNamespacesAsync(string monitoringAccount)
        {
            if (string.IsNullOrWhiteSpace(monitoringAccount)) throw new ArgumentException("monitoringAccount is null or empty.");

            var url = $"{Endpoint}/api/v1/hint/monitoringAccount/{monitoringAccount}/metricNamespace";

            var response = await GetResponse(
                new Uri(url),
                HttpMethod.Get,
                HttpClient,
                null,
                ClientId).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<string[]>(response.Item1);
        }

        /// <summary>
        /// Gets the list of metric names for the monitoringAccount and metricNamespace.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <returns>The list of metric names for the monitoringAccount and metricNamespace.</returns>
        public async Task<IEnumerable<string>> GetMetricNamesAsync(string monitoringAccount, string metricNamespace)
        {
            if (string.IsNullOrWhiteSpace(monitoringAccount)) throw new ArgumentException("monitoringAccount is null or empty.");

            if (string.IsNullOrWhiteSpace(metricNamespace)) throw new ArgumentException("metricNamespace is null or empty.");

            var url = $"{Endpoint}/api/v1/hint/monitoringAccount/{monitoringAccount}/metricNamespace/{EscapeTwice(metricNamespace)}/metric";

            var response = await GetResponse(
                new Uri(url),
                HttpMethod.Get,
                HttpClient,
                null,
                ClientId).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<string[]>(response.Item1);
        }

        /// <summary>
        /// Gets the list of dimension names for the metricId.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account.</param>
        /// <param name="metricNamespace">Metric namespace</param>
        /// <param name="metricName">Metric name</param>
        /// <returns>The list of dimension names for the metricId.</returns>
        public async Task<IEnumerable<string>> GetDimensionNamesAsync(string monitoringAccount, string metricNamespace, string metricName)
        {
            var url = $"{Endpoint}/api/v1/config/metrics/monitoringAccount/{monitoringAccount}/metricNamespace/{EscapeTwice(metricNamespace)}/metric/{EscapeTwice(metricName)}";

            var response = await GetResponse(
                new Uri(url),
                HttpMethod.Get,
                HttpClient,
                null,
                ClientId).ConfigureAwait(false);

            dynamic json = JsonConvert.DeserializeObject(response.Item1);
            var config = json.dimensionConfigurations;

            var names = new List<string>();
            foreach (dynamic token in config)
            {
                names.Add(token.id.Value);
            }

            return names;
        }

        /// <summary>
        /// Gets the dimension values for dimensionName satifying the dimensionFilters and
        /// </summary>
        /// <param name="metricId">Metric id.</param>
        /// <param name="filters">The dimension filters representing the pre-aggregate dimensions. Create an emtpy include filter for dimension with no filter values. Requested dimension should also be part of this and should be empty.</param>
        /// <param name="dimensionName">Name of the dimension for which values are requested.</param>
        /// <param name="startTimeUtc">Start time for evaluating dimension values.</param>
        /// <param name="endTimeUtc">End time for evaluating dimension values.</param>
        /// <returns>Dimension values for dimensionName.</returns>
        public async Task<IEnumerable<string>> GetDimensionValuesAsync(MetricIdentifier metricId, List<Tuple<string, IEnumerable<string>>> filters, string dimensionName, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            filters.Sort((item1, item2) => string.Compare(item1.Item1, item2.Item1, StringComparison.OrdinalIgnoreCase));

            var baseUrl = string.Format(
                "{0}/api/v2/hint/tenant/{1}/component/{2}/event/{3}/startTimeUtcMillis/{4}/endTimeUtcMillis/{5}",
                Endpoint,
                EscapeTwice(metricId.MonitoringAccount),
                EscapeTwice(metricId.MetricNamespace),
                EscapeTwice(metricId.MetricName),
                UnixEpochHelper.GetMillis(startTimeUtc),
                UnixEpochHelper.GetMillis(endTimeUtc));

            var urlBuilder = new StringBuilder(baseUrl);

            foreach (var filter in filters)
            {
                if (filter.Item1.Equals(dimensionName, StringComparison.OrdinalIgnoreCase) && filter.Item2.Count() > 0)
                {
                    throw new ArgumentException("Dimension filters cannot contain requested dimension with filter values");
                }

                if (filter.Item2.Count() > 0)
                {
                    foreach (var val in filter.Item2)
                    {
                        urlBuilder.Append("/").Append(EscapeTwice(filter.Item1)).Append("/").Append(EscapeTwice(val));
                    }
                }
                else
                {
                    // Put Empty value.
                    urlBuilder.Append("/").Append(EscapeTwice(filter.Item1)).Append("/").Append(EscapeTwice("{{*}}"));
                }
            }

            urlBuilder.Append("/").Append(EscapeTwice(dimensionName)).Append("/value");

            var response = await GetResponse(
                new Uri(urlBuilder.ToString()),
                HttpMethod.Get,
                HttpClient,
                null,
                ClientId).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<List<string>>(response.Item1);
        }

        /// <summary>
        /// Gets a list of the time series, each with multiple sampling types.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="aggregationType">The aggregation function used to reduce the resolution of the returned series.</param>
        /// <returns>The time series of for the given definitions.</returns>
        public async Task<IEnumerable<TimeSeries<MetricIdentifier, double?>>> GetMultipleTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, SamplingType[] samplingTypes, IEnumerable<TimeSeriesDefinition<MetricIdentifier>> definitions, int seriesResolutionInMinutes = 1, AggregationType aggregationType = AggregationType.Automatic)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            if (seriesResolutionInMinutes < 1)
            {
                throw new ArgumentException($"{seriesResolutionInMinutes} must be >= 1", nameof(seriesResolutionInMinutes));
            }

            var definitionList = definitions.ToList();

            if (definitionList.Count == 0)
            {
                throw new ArgumentException("The count of 'definitions' is 0.");
            }

            if (definitionList.Any(d => d == null))
            {
                throw new ArgumentException("At least one of definitions are null.");
            }

            if (startTimeUtc > endTimeUtc)
            {
                throw new ArgumentException(string.Format("startTimeUtc [{0}] must be <= endTimeUtc [{1}]", startTimeUtc, endTimeUtc));
            }

            NormalizeTimeRange(ref startTimeUtc, ref endTimeUtc);

            foreach (var def in definitionList)
            {
                def.SamplingTypes = samplingTypes;
                def.StartTimeUtc = startTimeUtc;
                def.EndTimeUtc = endTimeUtc;
                def.SeriesResolutionInMinutes = seriesResolutionInMinutes;
                def.AggregationType = aggregationType;
            }

            var monitoringAccount = definitionList[0].Id.MonitoringAccount;

            var url = string.Format(
                "{0}/api/v1/data/metrics/binary/version/3/monitoringAccount/{1}/returnMetricNames/{2}",
                Endpoint,
                monitoringAccount,
                false);

            var response = (await GetResponse(
                new Uri(url.ToString()),
                HttpMethod.Post,
                HttpClient,
                definitionList,
                ClientId).ConfigureAwait(false)).Item2;

            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                return MetricQueryResponseDeserializer.Deserialize(stream, definitionList.ToArray()).Item2;
            }
        }

        public async Task<MetricsClient.TimeSeries<Serialization.MetricIdentifier, double?>> GetTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, MetricsClient.Metrics.SamplingType[] samplingTypes, MetricsClient.TimeSeriesDefinition<Serialization.MetricIdentifier> definition, int seriesResolutionInMinutes = 1, MetricsClient.Query.AggregationType aggregationType = MetricsClient.Query.AggregationType.Automatic)
        {
            var response = await MetricReader.GetTimeSeriesAsync(startTimeUtc,
                endTimeUtc,
                samplingTypes,
                definition,
                seriesResolutionInMinutes,
                aggregationType).ConfigureAwait(false);

            return response;
        }

        public async Task<MetricsClient.TimeSeries<Serialization.MetricIdentifier, double?>> GetTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, MetricsClient.Metrics.SamplingType samplingType, MetricsClient.TimeSeriesDefinition<Serialization.MetricIdentifier> definition)
        {
            var response = await MetricReader.GetTimeSeriesAsync(startTimeUtc,
                endTimeUtc,
                samplingType,
                definition).ConfigureAwait(false);

            return response;

        }
        public async Task<MetricsClient.TimeSeries<Serialization.MetricIdentifier, double?>> GetTimeSeriesAsync(DateTime startTimeUtc,
            DateTime endTimeUtc,
            MetricsClient.Metrics.SamplingType samplingType,
            int seriesResolutionInMinutes,
            MetricsClient.TimeSeriesDefinition<Serialization.MetricIdentifier> definition)
        {
            var response = await MetricReader.GetTimeSeriesAsync(startTimeUtc,
                endTimeUtc,
                samplingType,
                seriesResolutionInMinutes,
                definition).ConfigureAwait(false); ;

            return response;
        }

        public async Task<IQueryResultListV3> GetTimeSeriesAsync(Serialization.MetricIdentifier metricId,
            IReadOnlyList<DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            IReadOnlyList<MetricsClient.Metrics.SamplingType> samplingTypes,
            SelectionClauseV3 selectionClause = null,
            MetricsClient.Query.AggregationType aggregationType = MetricsClient.Query.AggregationType.Automatic,
            long seriesResolutionInMinutes = 1,
            Guid? traceId = null,
            IReadOnlyList<string> outputDimensionNames = null,
            bool lastValueMode = false)
        {
            var response = await MetricReader.GetTimeSeriesAsync(metricId,
                dimensionFilters,
                startTimeUtc,
                endTimeUtc,
                samplingTypes,
                selectionClause,
                aggregationType,
                seriesResolutionInMinutes,
                traceId,
                outputDimensionNames,
                lastValueMode).ConfigureAwait(false); ;

            return response;
        }
        private static void NormalizeTimeRange(ref DateTime startTimeUtc, ref DateTime endTimeUtc)
        {
            startTimeUtc = new DateTime(startTimeUtc.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
            endTimeUtc = new DateTime(endTimeUtc.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
        }

        private static string EscapeTwice(string str)
        {
            return Uri.EscapeDataString(Uri.EscapeDataString(str));
        }

        /// <summary>
        /// Creates the HTTP client
        /// </summary>
        /// <returns>
        /// An instance of <see cref="HttpClient" />
        /// </returns>
        private static HttpClient CreateHttpClient(X509Certificate2 certificate)
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
        private async Task<Tuple<string, HttpResponseMessage>> GetResponse(
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

                    DiagnosticsETWProvider.Instance.LogDataProviderMessage(RequestId, "MdmClient", $"GetResponse Failure. Latency: {stopWatch.Elapsed}; Number of attempts: {i}; Exception: {e.ToString()}");

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

        private async Task<Tuple<string, HttpResponseMessage>> GetResponseNoRetry(
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
            string message = null;
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

                message = $"Succeeded to get a response from the server. Url: {request.RequestUri}, HandlingServer:{handlingServer}";

                return Tuple.Create(responseString, response);
            }
            catch (Exception e)
            {
                message = $"Failed to get a response from the server. Url:{request.RequestUri}, HandlingServer:{handlingServer} Stage:{stage}, "
                    + $"ResponseStatus:{response?.StatusCode.ToString() ?? "<none>"}";

                throw new MetricsClientException(message, e, traceId.Value, response?.StatusCode);
            }
            finally
            {
                DiagnosticsETWProvider.Instance.LogDataProviderMessage(RequestId, "MdmClient", message);
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
