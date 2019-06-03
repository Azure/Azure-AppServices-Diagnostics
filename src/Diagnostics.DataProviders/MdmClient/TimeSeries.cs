using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// A class representing a time series.
    /// </summary>
    /// <typeparam name="TId"><see cref="MetricIdentifier"/> or <see cref="MonitorIdentifier"/>.</typeparam>
    /// <typeparam name="TValue">The type of time series values.</typeparam>
    public sealed class TimeSeries<TId, TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSeries{ID, TValue}" /> class.
        /// </summary>
        /// <param name="startTimeUtc">The start time in UTC.</param>
        /// <param name="endTimeUtc">The end time in UTC.</param>
        /// <param name="seriesResolutionInMinutes">The series resolution in minutes.</param>
        /// <param name="definition">The time series definition.</param>
        /// <param name="values">The time series values.</param>
        /// <param name="errorCode">The error code.</param>
        internal TimeSeries(DateTime startTimeUtc, DateTime endTimeUtc, int seriesResolutionInMinutes, TimeSeriesDefinition<TId> definition, List<List<TValue>> values, TimeSeriesErrorCode errorCode)
        {
            this.Definition = definition;
            this.StartTimeUtc = startTimeUtc;
            this.EndTimeUtc = endTimeUtc;
            this.SeriesResolutionInMinutes = seriesResolutionInMinutes;
            this.Values = values;
            this.ErrorCode = errorCode;
        }

        /// <summary>
        /// Gets the time series definition.
        /// </summary>
        public TimeSeriesDefinition<TId> Definition { get; private set; }

        /// <summary>
        /// Gets the start time UTC, inclusive.
        /// </summary>
        public DateTime StartTimeUtc { get; internal set; }

        /// <summary>
        /// Gets the end time UTC, inclusive.
        /// </summary>
        public DateTime EndTimeUtc { get; internal set; }

        /// <summary>
        /// Gets the error code if any.
        /// </summary>
        public TimeSeriesErrorCode ErrorCode { get; internal set; }

        /// <summary>
        /// Gets the resolution window used to reduce the resolution of the returned series.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(1)]
        public int SeriesResolutionInMinutes { get; internal set; }

        /// <summary>
        /// Gets the datapoints.
        /// </summary>
        /// <returns>an enumerable of datapoints.</returns>
        public IEnumerable<Datapoint<TValue>> Datapoints
        {
            get
            {
                // We supported only one samplying type in old APIs, and "SamplingTypes" was not a member of TimeSeriesDefinition<TId>.
                return this.GetDatapoints(0);
            }
        }

        /// <summary>
        /// The time series values returned from metric server.
        /// </summary>
        /// <remarks>Expose the raw data to public usage so that we don't need to generate many <see cref="Datapoint{T}"/> objects.</remarks>
        public IReadOnlyList<IReadOnlyList<TValue>> RawValues
        {
            get
            {
                return this.Values;
            }
        }

        /// <summary>
        /// The time series values returned from metric server.
        /// </summary>
        /// <remarks>For internal use so that we don't need to generate many <see cref="Datapoint{T}"/> objects.</remarks>
        internal List<List<TValue>> Values { get; set; }

        /// <summary>
        /// Gets the datapoints for a given sampling type.
        /// </summary>
        /// <param name="samplingType">Type of the sampling.</param>
        /// <returns>
        /// an enumerable of datapoints.
        /// </returns>
        /// <exception cref="System.ArgumentException">samplingType</exception>
        public IEnumerable<Datapoint<TValue>> GetDatapoints(SamplingType samplingType)
        {
            var index = Array.IndexOf(this.Definition.SamplingTypes, samplingType);

            if (index < 0)
            {
                var message = $"'{samplingType}' is not found in [{string.Join(",", this.Definition.SamplingTypes)}])";
                throw new ArgumentException(message, nameof(samplingType));
            }

            return this.GetDatapoints(index);
        }

        /// <summary>
        /// Gets the datapoints for the given index to the sampling types.
        /// </summary>
        /// <param name="indexOfSamplingTypes">The index of sampling types.</param>
        /// <returns>
        /// The datapoints for the given index to the sampling types.
        /// </returns>
        public IEnumerable<Datapoint<TValue>> GetDatapoints(int indexOfSamplingTypes)
        {
            if (this.Values != null)
            {
                var numSamplingTypes = this.Values.Count;
                if (indexOfSamplingTypes >= numSamplingTypes)
                {
                    throw new ArgumentOutOfRangeException(nameof(indexOfSamplingTypes), $"indexOfSamplingTypes = {indexOfSamplingTypes}, numSamplingTypes = {numSamplingTypes}.");
                }

                if (this.Values[indexOfSamplingTypes] != null)
                {
                    for (int i = 0; i < this.Values[indexOfSamplingTypes].Count; ++i)
                    {
                        yield return new Datapoint<TValue>(this.StartTimeUtc.AddMinutes(i * this.SeriesResolutionInMinutes), this.Values[indexOfSamplingTypes][i]);
                    }
                }
            }
        }
    }
}
