using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.DataProviders.Utility;
using Diagnostics.Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

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
        public HttpClient HttpClient { get; private set; }

        /// <summary>
        /// Connection info
        /// </summary>
        public ConnectionInfo ConnectionInfo { get; private set; }

        private readonly string ClientId = "ClientAPI";

        /// <summary>
        /// Initializes a new instance of the <see cref="MdmClient" /> class.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        public MdmClient(MdmDataProviderConfiguration configuration, string requestId)
        {
            try
            {
                ConnectionInfo = new ConnectionInfo(new Uri(configuration.Endpoint), configuration.CertificateThumbprint);
                HttpClient = HttpClientHelper.CreateHttpClientWithAuthInfo(ConnectionInfo.Certificate);
                RequestId = requestId;
            }
            catch (Exception ex)
            {
                // Log failure 
                DiagnosticsETWProvider.Instance.LogDataProviderException(requestId, "Initialize MDM data provider", DateTime.UtcNow.ToString(), ex.GetType().ToString(), ex.ToString());
            }
        }

        /// <summary>
        /// Gets the list of namespaces for the monitoringAccount.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <returns>The list of namespaces for the monitoringAccount.</returns>
        public async Task<IEnumerable<string>> GetNamespacesAsync(string monitoringAccount)
        {
            if (string.IsNullOrWhiteSpace(monitoringAccount)) throw new ArgumentException("monitoringAccount is null or empty.");

            var url = $"{ConnectionInfo.GetEndpoint(monitoringAccount)}/api/v1/hint/monitoringAccount/{monitoringAccount}/metricNamespace";

            var response = await HttpClientHelper.GetResponse(
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

            var url = $"{ConnectionInfo.GetEndpoint(monitoringAccount)}/api/v1/hint/monitoringAccount/{monitoringAccount}/metricNamespace/{EscapeTwice(metricNamespace)}/metric";

            var response = await HttpClientHelper.GetResponse(
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
            var url = $"{ConnectionInfo.GetEndpoint(monitoringAccount)}/api/v1/config/metrics/monitoringAccount/{monitoringAccount}/metricNamespace/{EscapeTwice(metricNamespace)}/metric/{EscapeTwice(metricName)}";

            var response = await HttpClientHelper.GetResponse(
                new Uri(url),
                HttpMethod.Get,
                HttpClient,
                null,
                ClientId).ConfigureAwait(false);

            dynamic json = JsonConvert.DeserializeObject(response.Item1);
            var config = json.dimensionConfigurations;

            var names = new List<string>();
            foreach(dynamic token in config)
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
                ConnectionInfo.GetEndpoint(metricId.MonitoringAccount),
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

            var response = await HttpClientHelper.GetResponse(
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
                ConnectionInfo.GetMetricsDataQueryEndpoint(monitoringAccount).OriginalString,
                monitoringAccount,
                false);

            var response = (await HttpClientHelper.GetResponse(
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

        private static void NormalizeTimeRange(ref DateTime startTimeUtc, ref DateTime endTimeUtc)
        {
            startTimeUtc = new DateTime(startTimeUtc.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
            endTimeUtc = new DateTime(endTimeUtc.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
        }

        private static string EscapeTwice(string str)
        {
            return Uri.EscapeDataString(Uri.EscapeDataString(str));
        }
    }
}
