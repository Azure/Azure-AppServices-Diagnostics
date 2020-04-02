using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models;
using Microsoft.AspNetCore.Http;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Mdm data provider
    /// </summary>
    public class MdmDataProvider : DiagnosticDataProvider, IDiagnosticDataProvider, IMdmDataProvider
    {
        private IMdmDataProviderConfiguration _configuration;
        private IMdmClient _mdmClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MdmDataProvider"/> class.
        /// </summary>
        /// <param name="cache">Operation cache.</param>
        /// <param name="configuration">Data provider configuration.</param>
        /// <param name="requestId">Request id.</param>
        public MdmDataProvider(OperationDataCache cache, IMdmDataProviderConfiguration configuration, string requestId, IHeaderDictionary headers = null)
            : base(cache)
        {
            _configuration = configuration;
            _mdmClient = MdmClientFactory.GetMdmClient(configuration, requestId);
        }

        /// <summary>
        /// Get metadata.
        /// </summary>
        /// <returns>Data provider metadata.</returns>
        public DataProviderMetadata GetMetadata()
        {
            return null;
        }

        /// <summary>
        /// Gets the list of namespaces for the monitoringAccount.
        /// </summary>
        /// <returns>The list of namespaces for the monitoringAccount.</returns>
        public async Task<IEnumerable<string>> GetNamespacesAsync()
        {
            return await _mdmClient.GetNamespacesAsync(_configuration.MonitoringAccount);
        }

        /// <summary>
        /// Gets the list of metric names for the monitoringAccount and metricNamespace.
        /// </summary>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <returns>The list of metric names for the monitoringAccount and metricNamespace.</returns>
        public async Task<IEnumerable<string>> GetMetricNamesAsync(string metricNamespace)
        {
            return await _mdmClient.GetMetricNamesAsync(_configuration.MonitoringAccount, metricNamespace);
        }

        /// <summary>
        /// Gets the list of dimension names for the metricId.
        /// </summary>
        /// <param name="metricNamespace">Metric namespace</param>
        /// <param name="metricName">Metric name</param>
        /// <returns>The list of dimension names for the metricId.</returns>
        public async Task<IEnumerable<string>> GetDimensionNamesAsync(string metricNamespace, string metricName)
        {
            return await _mdmClient.GetDimensionNamesAsync(_configuration.MonitoringAccount, metricNamespace, metricName);
        }

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
        public async Task<IEnumerable<string>> GetDimensionValuesAsync(string metricNamespace, string metricName, List<Tuple<string, IEnumerable<string>>> filter, string dimensionName, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            var contain = false;
            foreach (var f in filter)
            {
                if (f.Item1.Equals(dimensionName, StringComparison.OrdinalIgnoreCase))
                {
                    contain = true;
                    break;
                }
            }

            if (!contain)
            {
                filter.Add(Tuple.Create(dimensionName, (IEnumerable<string>)new string[] { }));
            }

            return await _mdmClient.GetDimensionValuesAsync(new MetricIdentifier(_configuration.MonitoringAccount, metricNamespace, metricName), filter, dimensionName, startTimeUtc, endTimeUtc);
        }

        /// <summary>
        /// Gets the dimension values for dimensionName satifying the dimensionFilters and
        /// </summary>
        /// <param name="metricNamespace">Metric namespace</param>
        /// <param name="metricName">Metric name</param>
        /// <param name="dimensionName">Name of the dimension for which values are requested.</param>
        /// <param name="startTimeUtc">Start time for evaluating dimension values.</param>
        /// <param name="endTimeUtc">End time for evaluating dimension values.</param>
        /// <returns>Dimension values for dimensionName.</returns>
        public async Task<IEnumerable<string>> GetDimensionValuesAsync(string metricNamespace, string metricName, string dimensionName, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            var filter = new List<Tuple<string, IEnumerable<string>>>
            {
                Tuple.Create(dimensionName, (IEnumerable<string>)new string[] { })
            };

            return await _mdmClient.GetDimensionValuesAsync(new MetricIdentifier(_configuration.MonitoringAccount, metricNamespace, metricName), filter, dimensionName, startTimeUtc, endTimeUtc);
        }

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
        public async Task<IEnumerable<DataTable>> GetTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, Sampling sampling, string metricNamespace, string metricName, IDictionary<string, string> dimension)
        {
            var definition = Tuple.Create<string, string, IEnumerable<KeyValuePair<string, string>>>(metricNamespace, metricName, dimension);
            return await GetMultipleTimeSeriesAsync(startTimeUtc, endTimeUtc, sampling, new List<Tuple<string, string, IEnumerable<KeyValuePair<string, string>>>> { definition });
        }

        /// <summary>
        /// Gets a list of the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="sampling">The sampling type.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <returns>The time series of for the given definitions.</returns>
        public async Task<IEnumerable<DataTable>> GetMultipleTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, Sampling sampling, int seriesResolutionInMinutes, IEnumerable<Tuple<string, string, IEnumerable<KeyValuePair<string, string>>>> definitions)
        {
            return await GetMultipleTimeSeriesAsync(startTimeUtc, endTimeUtc, sampling, definitions, seriesResolutionInMinutes);
        }

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
        public async Task<IEnumerable<DataTable>> GetMultipleTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, Sampling sampling, IEnumerable<Tuple<string, string, IEnumerable<KeyValuePair<string, string>>>> definitions, int seriesResolutionInMinutes = 1, AggregationType aggregationType = AggregationType.Automatic)
        {
            // Create sampling type.
            var samplingType = sampling.ToSamplingType();

            // Create time series definition.
            var _definitions = new List<TimeSeriesDefinition<MetricIdentifier>>();
            foreach (var def in definitions)
            {
                _definitions.Add(
                    new TimeSeriesDefinition<MetricIdentifier>(
                        new MetricIdentifier(_configuration.MonitoringAccount, def.Item1, def.Item2),
                        def.Item3
                ));
            }

            var series = await _mdmClient.GetMultipleTimeSeriesAsync(
                startTimeUtc,
                endTimeUtc,
                samplingType.ToArray(),
                _definitions,
                seriesResolutionInMinutes,
                aggregationType);

            var result = new List<DataTable>();

            // Generate data table.
            foreach (var serie in series)
            {
                foreach (var s in samplingType)
                {
                    var table = new DataTable();
                    table.Columns.Add("TimeStamp", typeof(DateTime));
                    table.Columns.Add("Metric", typeof(string));
                    table.Columns.Add(s.ToString(), typeof(double));

                    foreach (var point in serie.GetDatapoints(s))
                    {
                        table.Rows.Add(new object[] { point.TimestampUtc, serie.Definition.Id.MetricName, point.Value });
                    }

                    result.Add(table);
                }
            }

            return result;
        }
    }
}
