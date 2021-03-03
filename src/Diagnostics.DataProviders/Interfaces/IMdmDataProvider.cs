using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MetricsClient = Microsoft.Cloud.Metrics.Client;
using MetricsClientConfiguration = Microsoft.Online.Metrics.Serialization.Configuration;

namespace Diagnostics.DataProviders.Interfaces
{
    public interface IMdmDataProvider : IMetadataProvider
    {
        /// <summary>
        /// Gets the list of namespaces for the monitoringAccount.
        /// </summary>
        /// <returns>The list of namespaces for the monitoringAccount.</returns>
        Task<IEnumerable<string>> GetNamespacesAsync();

        /// <summary>
        /// Gets the list of metric names for the monitoringAccount and metricNamespace.
        /// </summary>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <returns>The list of metric names for the monitoringAccount and metricNamespace.</returns>
        Task<IEnumerable<string>> GetMetricNamesAsync(string metricNamespace);

        /// <summary>
        /// Gets the list of dimension names for the metricId.
        /// </summary>
        /// <param name="metricNamespace">Metric namespace</param>
        /// <param name="metricName">Metric name</param>
        /// <returns>The list of dimension names for the metricId.</returns>
        Task<IEnumerable<string>> GetDimensionNamesAsync(string metricNamespace, string metricName);

        /// <summary>
        /// Gets the dimension values for dimensionName satifying the dimensionFilters and
        /// </summary>
        /// <param name="metricNamespace">Metric namespace</param>
        /// <param name="metricName">Metric name</param>
        /// <param name="filter">The dimension filters representing the pre-aggregate dimensions. Create an emtpy include filter for dimension with no filter values. Requested dimension should also be part of this and should be empty.</param>
        /// <param name="dimensionName">Name of the dimension for which values are requested.</param>
        /// <param name="startTimeUtc">Start time for evaluating dimension values.</param>
        /// <param name="endTimeUtc">End time for evaluating dimension values.</param>
        /// <returns>Dimension values for dimensionName.</returns>
        Task<IEnumerable<string>> GetDimensionValuesAsync(string metricNamespace, string metricName, List<Tuple<string, IEnumerable<string>>> filter, string dimensionName, DateTime startTimeUtc, DateTime endTimeUtc);

        /// <summary>
        /// Gets the dimension values for dimensionName satifying the dimensionFilters and
        /// </summary>
        /// <param name="metricNamespace">Metric namespace</param>
        /// <param name="metricName">Metric name</param>
        /// <param name="dimensionName">Name of the dimension for which values are requested.</param>
        /// <param name="startTimeUtc">Start time for evaluating dimension values.</param>
        /// <param name="endTimeUtc">End time for evaluating dimension values.</param>
        /// <returns>Dimension values for dimensionName.</returns>
        Task<IEnumerable<string>> GetDimensionValuesAsync(string metricNamespace, string metricName, string dimensionName, DateTime startTimeUtc, DateTime endTimeUtc);

        /// <summary>
        /// Gets the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="sampling">The sampling type.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">The metric name.</param>
        /// <param name="dimension">The dimension.</param>
        /// <returns>The time series for the given definition.</returns>
        Task<IEnumerable<DataTable>> GetTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, Sampling sampling, string metricNamespace, string metricName, IDictionary<string, string> dimension);

        /// <summary>
        /// Gets the time series with specified time granularity.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="sampling">The sampling type.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">The metric name.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="dimension">The dimension.</param>
        /// <returns>The time series for the given definition.</returns>
        Task<IEnumerable<DataTable>> GetTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, Sampling sampling, string metricNamespace, string metricName, int seriesResolutionInMinutes, IDictionary<string, string> dimension);        

        /// <summary>
        /// Gets a list of the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="sampling">The sampling type.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <returns>The time series of for the given definitions.</returns>
        Task<IEnumerable<DataTable>> GetMultipleTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, Sampling sampling, int seriesResolutionInMinutes, IEnumerable<Tuple<string, string, IEnumerable<KeyValuePair<string, string>>>> definitions);

        /// <summary>
        /// Gets a list of the time series, each with multiple sampling types.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="sampling">The sampling types.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="aggregationType">The aggregation function used to reduce the resolution of the returned series.</param>
        /// <returns>The time series of for the given definitions.</returns>
        Task<IEnumerable<DataTable>> GetMultipleTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, Sampling sampling, IEnumerable<Tuple<string, string, IEnumerable<KeyValuePair<string, string>>>> definitions, int seriesResolutionInMinutes = 1, AggregationType aggregationType = AggregationType.Automatic);


        Task<MetricsClient.TimeSeries<MetricsClientConfiguration.MetricIdentifier, double?>> GetTimeSeriesAsync(DateTime startTimeUtc,
            DateTime endTimeUtc,
            MetricsClient.Metrics.SamplingType samplingType,
            int seriesResolutionInMinutes,
            MetricsClient.TimeSeriesDefinition<MetricsClientConfiguration.MetricIdentifier> definition);


        Task<MetricsClient.TimeSeries<MetricsClientConfiguration.MetricIdentifier, double?>> GetTimeSeriesAsync(DateTime startTimeUtc,
            DateTime endTimeUtc,
            MetricsClient.Metrics.SamplingType[] samplingTypes,
            MetricsClient.TimeSeriesDefinition<MetricsClientConfiguration.MetricIdentifier> definition,
            int seriesResolutionInMinutes = 1,
            MetricsClient.Query.AggregationType aggregationType = MetricsClient.Query.AggregationType.Automatic);

        Task<MetricsClient.TimeSeries<MetricsClientConfiguration.MetricIdentifier, double?>> GetTimeSeriesAsync(DateTime startTimeUtc,
            DateTime endTimeUtc,
            MetricsClient.Metrics.SamplingType samplingType,
            MetricsClient.TimeSeriesDefinition<MetricsClientConfiguration.MetricIdentifier> definition);

        Task<MetricsClient.Query.IQueryResultListV3> GetTimeSeriesAsync(MetricsClientConfiguration.MetricIdentifier metricId,
            IReadOnlyList<MetricsClient.Metrics.DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            IReadOnlyList<MetricsClient.Metrics.SamplingType> samplingTypes,
            MetricsClient.Query.SelectionClauseV3 selectionClause = null,
            MetricsClient.Query.AggregationType aggregationType = MetricsClient.Query.AggregationType.Automatic,
            long seriesResolutionInMinutes = 1,
            Guid? traceId = null,
            IReadOnlyList<string> outputDimensionNames = null,
            bool lastValueMode = false);

    }
}
