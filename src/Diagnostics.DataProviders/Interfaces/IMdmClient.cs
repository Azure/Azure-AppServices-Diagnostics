﻿using Microsoft.Cloud.Metrics.Client;
using Microsoft.Cloud.Metrics.Client.Metrics;
using Microsoft.Cloud.Metrics.Client.Query;
using Microsoft.Online.Metrics.Serialization.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders.Interfaces
{
    /// <summary>
    /// Interface for MDM client
    /// </summary>
    public interface IMdmClient
    {
        /// <summary>
        /// Gets the list of namespaces for the monitoringAccount.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <returns>The list of namespaces for the monitoringAccount.</returns>
        Task<IEnumerable<string>> GetNamespacesAsync(string monitoringAccount);

        /// <summary>
        /// Gets the list of metric names for the monitoringAccount and metricNamespace.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <returns>The list of metric names for the monitoringAccount and metricNamespace.</returns>
        Task<IEnumerable<string>> GetMetricNamesAsync(string monitoringAccount, string metricNamespace);

        /// <summary>
        /// Gets the list of dimension names for the metricId.
        /// </summary>
        /// <param name="monitoringAccount">Monitoring account.</param>
        /// <param name="metricNamespace">Metric namespace</param>
        /// <param name="metricName">Metric name</param>
        /// <returns>The list of dimension names for the metricId.</returns>
        Task<IEnumerable<string>> GetDimensionNamesAsync(string monitoringAccount, string metricNamespace, string metricName);

        /// <summary>
        /// Gets the dimension values for dimensionName satifying the dimensionFilters and
        /// </summary>
        /// <param name="metricId">Metric id.</param>
        /// <param name="dimensionFilters">The dimension filters representing the pre-aggregate dimensions. Create an emtpy include filter for dimension with no filter values. Requested dimension should also be part of this and should be empty.</param>
        /// <param name="dimensionName">Name of the dimension for which values are requested.</param>
        /// <param name="startTimeUtc">Start time for evaluating dimension values.</param>
        /// <param name="endTimeUtc">End time for evaluating dimension values.</param>
        /// <returns>Dimension values for dimensionName.</returns>
        Task<IEnumerable<string>> GetDimensionValuesAsync(MetricIdentifier metricId, List<DimensionFilter> dimensionFilters, string dimensionName, DateTime startTimeUtc, DateTime endTimeUtc);

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
        Task<IEnumerable<TimeSeries<MetricIdentifier, double?>>> GetMultipleTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, SamplingType[] samplingTypes, IEnumerable<TimeSeriesDefinition<MetricIdentifier>> definitions, int seriesResolutionInMinutes = 1, AggregationType aggregationType = AggregationType.Automatic);
    }
}
