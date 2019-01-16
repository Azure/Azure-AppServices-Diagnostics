using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.Logger;
using Microsoft.Cloud.Metrics.Client;
using Microsoft.Cloud.Metrics.Client.Metrics;
using Microsoft.Cloud.Metrics.Client.Query;
using Microsoft.Online.Metrics.Serialization.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public class MdmClient : IMdmClient
    {
        /// <summary>
        /// Gets the metric reader.
        /// </summary>
        public MetricReader Reader { get; private set; }

        /// <summary>
        /// Gets the request id.
        /// </summary>
        public string RequestId { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MdmClient" /> class.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        public MdmClient(MdmDataProviderConfiguration configuration, string requestId)
        {
            try
            {
                var connectionInfo = new ConnectionInfo(new Uri(configuration.Endpoint), configuration.CertificateThumbprint);
                Reader = new MetricReader(connectionInfo);
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
            return await Reader.GetNamespacesAsync(monitoringAccount);
        }

        /// <summary>
        /// Gets the list of metric names for the monitoringAccount and metricNamespace.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <returns>The list of metric names for the monitoringAccount and metricNamespace.</returns>
        public async Task<IEnumerable<string>> GetMetricNamesAsync(string monitoringAccount, string metricNamespace)
        {
            return await Reader.GetMetricNamesAsync(monitoringAccount, metricNamespace);
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
            var id = new MetricIdentifier(monitoringAccount, metricNamespace, metricName);
            return await Reader.GetDimensionNamesAsync(id);
        }

        /// <summary>
        /// Gets the dimension values for dimensionName satifying the dimensionFilters and
        /// </summary>
        /// <param name="metricId">Metric id.</param>
        /// <param name="dimensionFilters">The dimension filters representing the pre-aggregate dimensions. Create an emtpy include filter for dimension with no filter values. Requested dimension should also be part of this and should be empty.</param>
        /// <param name="dimensionName">Name of the dimension for which values are requested.</param>
        /// <param name="startTimeUtc">Start time for evaluating dimension values.</param>
        /// <param name="endTimeUtc">End time for evaluating dimension values.</param>
        /// <returns>Dimension values for dimensionName.</returns>
        public async Task<IEnumerable<string>> GetDimensionValuesAsync(MetricIdentifier metricId, List<DimensionFilter> dimensionFilters, string dimensionName, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            return await Reader.GetDimensionValuesAsync(metricId, dimensionFilters, dimensionName, startTimeUtc, endTimeUtc);
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
            return await Reader.GetMultipleTimeSeriesAsync(startTimeUtc, endTimeUtc, samplingTypes, definitions, seriesResolutionInMinutes, aggregationType);
        }
    }
}
