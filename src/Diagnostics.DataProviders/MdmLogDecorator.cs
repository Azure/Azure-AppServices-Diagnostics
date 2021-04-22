using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;
using MetricsClient = Microsoft.Cloud.Metrics.Client;
using Serialization = Microsoft.Online.Metrics.Serialization;

namespace Diagnostics.DataProviders
{
	internal class MdmLogDecorator : LogDecoratorBase, IMdmDataProvider
	{
		public IMdmDataProvider DataProvider;

		public MdmLogDecorator(DataProviderContext context, IMdmDataProvider dataProvider) : base(dataProvider as DiagnosticDataProvider, context, dataProvider.GetMetadata())
		{
			DataProvider = dataProvider;
		}

		public Task<IEnumerable<string>> GetNamespacesAsync()
		{
			return MakeDependencyCall(DataProvider.GetNamespacesAsync());
		}

		public Task<IEnumerable<string>> GetMetricNamesAsync(string metricNamespace)
		{
			return MakeDependencyCall(DataProvider.GetMetricNamesAsync(metricNamespace));
		}

		public Task<IEnumerable<string>> GetDimensionNamesAsync(string metricNamespace, string metricName)
		{
			return MakeDependencyCall(DataProvider.GetDimensionNamesAsync(metricNamespace, metricName));
		}

		public Task<IEnumerable<string>> GetDimensionValuesAsync(string metricNamespace, string metricName, List<Tuple<string, IEnumerable<string>>> filter, string dimensionName, DateTime startTimeUtc, DateTime endTimeUtc)
		{
			return MakeDependencyCall(DataProvider.GetDimensionValuesAsync(metricNamespace, metricName, filter, dimensionName, startTimeUtc, endTimeUtc));
		}

		public Task<IEnumerable<string>> GetDimensionValuesAsync(string metricNamespace, string metricName, string dimensionName, DateTime startTimeUtc, DateTime endTimeUtc)
		{
			return MakeDependencyCall(DataProvider.GetDimensionValuesAsync(metricNamespace, metricName, dimensionName, startTimeUtc, endTimeUtc));
		}

		public Task<IEnumerable<DataTable>> GetTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, Sampling sampling, string metricNamespace, string metricName, IDictionary<string, string> dimension)
		{
			return MakeDependencyCall(DataProvider.GetTimeSeriesAsync(startTimeUtc, endTimeUtc, sampling, metricNamespace, metricName, dimension));
		}

        public Task<IEnumerable<DataTable>> GetTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, Sampling sampling, string metricNamespace, string metricName, int seriesResolutionInMinutes, IDictionary<string, string> dimension)
        {
            return MakeDependencyCall(DataProvider.GetTimeSeriesAsync(startTimeUtc, endTimeUtc, sampling, metricNamespace, metricName, seriesResolutionInMinutes, dimension));
        }

        public Task<IEnumerable<DataTable>> GetMultipleTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, Sampling sampling, int seriesResolutionInMinutes, IEnumerable<Tuple<string, string, IEnumerable<KeyValuePair<string, string>>>> definitions)
		{
			return MakeDependencyCall(DataProvider.GetMultipleTimeSeriesAsync(startTimeUtc, endTimeUtc, sampling, seriesResolutionInMinutes, definitions));
		}

		public Task<IEnumerable<DataTable>> GetMultipleTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, Sampling sampling, IEnumerable<Tuple<string, string, IEnumerable<KeyValuePair<string, string>>>> definitions, int seriesResolutionInMinutes = 1, AggregationType aggregationType = AggregationType.Automatic)
		{
			return MakeDependencyCall(DataProvider.GetMultipleTimeSeriesAsync(startTimeUtc, endTimeUtc, sampling, definitions, seriesResolutionInMinutes, aggregationType));
		}

        public Task<MetricsClient.TimeSeries<Serialization.Configuration.MetricIdentifier, double?>> GetTimeSeriesAsync(DateTime startTimeUtc,
            DateTime endTimeUtc,
            MetricsClient.Metrics.SamplingType samplingType,
            int seriesResolutionInMinutes,
            MetricsClient.TimeSeriesDefinition<Serialization.Configuration.MetricIdentifier> definition)
        {
			return MakeDependencyCall(DataProvider.GetTimeSeriesAsync(startTimeUtc, endTimeUtc, samplingType, definition));
        }

        public Task<MetricsClient.TimeSeries<Serialization.Configuration.MetricIdentifier, double?>> GetTimeSeriesAsync(DateTime startTimeUtc,
            DateTime endTimeUtc,
            MetricsClient.Metrics.SamplingType[] samplingTypes,
            MetricsClient.TimeSeriesDefinition<Serialization.Configuration.MetricIdentifier> definition,
            int seriesResolutionInMinutes = 1,
            MetricsClient.Query.AggregationType aggregationType = MetricsClient.Query.AggregationType.Automatic)
        {
			return MakeDependencyCall(DataProvider.GetTimeSeriesAsync(startTimeUtc,
                endTimeUtc,
                samplingTypes,
                definition,
                seriesResolutionInMinutes,
                aggregationType));
        }

        public Task<MetricsClient.TimeSeries<Serialization.Configuration.MetricIdentifier, double?>> GetTimeSeriesAsync(DateTime startTimeUtc,
            DateTime endTimeUtc,
            MetricsClient.Metrics.SamplingType samplingType,
            MetricsClient.TimeSeriesDefinition<Serialization.Configuration.MetricIdentifier> definition)
        {
			return MakeDependencyCall(DataProvider.GetTimeSeriesAsync(startTimeUtc, endTimeUtc, samplingType, definition));
        }

        public Task<MetricsClient.Query.IQueryResultListV3> GetTimeSeriesAsync(Serialization.Configuration.MetricIdentifier metricId,
            IReadOnlyList<MetricsClient.Metrics.DimensionFilter> dimensionFilters,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            IReadOnlyList<MetricsClient.Metrics.SamplingType> samplingTypes,
            MetricsClient.Query.SelectionClauseV3 selectionClause = null,
            MetricsClient.Query.AggregationType aggregationType = MetricsClient.Query.AggregationType.Automatic,
            long seriesResolutionInMinutes = 1,
            Guid? traceId = null,
            IReadOnlyList<string> outputDimensionNames = null,
            bool lastValueMode = false)
        {
			return MakeDependencyCall(DataProvider.GetTimeSeriesAsync(metricId,
                dimensionFilters,
                startTimeUtc,
                endTimeUtc,
                samplingTypes,
                selectionClause,
                aggregationType,
                seriesResolutionInMinutes,
                traceId,
                outputDimensionNames,
                lastValueMode));
        }
    }
}
