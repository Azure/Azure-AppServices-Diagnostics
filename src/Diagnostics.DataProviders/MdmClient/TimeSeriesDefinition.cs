using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// A class representing a time series definition.
    /// </summary>
    /// <typeparam name="TId"><see cref="MetricIdentifier"/> or <see cref="MonitorIdentifier"/>.</typeparam>
    public sealed class TimeSeriesDefinition<TId>
    {
        /// <summary>
        /// The dimension name and the dimension value pairs.
        /// </summary>
        private KeyValuePair<string, string>[] dimensionCombination;

        /// <summary>
        /// <see cref="MetricIdentifier"/> or <see cref="MonitorIdentifier"/>.
        /// </summary>
        private TId id;

        /// <summary>
        /// The start time UTC.
        /// </summary>
        private DateTime startTimeUtc;

        /// <summary>
        /// The end time UTC.
        /// </summary>
        private DateTime endTimeUtc;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSeriesDefinition{T}"/> class.
        /// </summary>
        /// <param name="id"><see cref="MetricIdentifier"/> or <see cref="MonitorIdentifier"/>.</param>
        /// <param name="dimensionCombination">The dimension name and the dimension value pairs.</param>
        /// <remarks>Empty or null for <paramref name="dimensionCombination"/> means the "Total" pre-aggregate.</remarks>
        public TimeSeriesDefinition(TId id, IEnumerable<KeyValuePair<string, string>> dimensionCombination)
        {
            this.id = id;
            this.dimensionCombination = dimensionCombination != null ? dimensionCombination.ToArray() : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSeriesDefinition{T}"/> class.
        /// </summary>
        /// <param name="id"><see cref="MetricIdentifier"/> or <see cref="MonitorIdentifier"/>.</param>
        /// <param name="dimensionCombination">The dimension name and the dimension value pairs.</param>
        /// <remarks>Empty <paramref name="dimensionCombination"/> means the "Total" pre-aggregate.</remarks>
        public TimeSeriesDefinition(TId id, params KeyValuePair<string, string>[] dimensionCombination)
        {
            this.id = id;
            this.dimensionCombination = dimensionCombination;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSeriesDefinition{TId}" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="dimensionCombination">The dimension combination.</param>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="aggregationType">The aggregation function used to reduce the resolution of the returned series.</param>
        /// <param name="zeroAsNoValueSentinel">
        /// if <see langword="true"/>, 0 is treated as the sentinel when the system has not received any metrics in a minutely window;
        /// if <see langword="false"/>, the sentinel is null.
        /// </param>
        [JsonConstructor]
        public TimeSeriesDefinition(
            TId id,
            KeyValuePair<string, string>[] dimensionCombination,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType[] samplingTypes,
            int seriesResolutionInMinutes = 1,
            AggregationType aggregationType = AggregationType.None,
            bool zeroAsNoValueSentinel = false)
        {
            this.id = id;
            this.dimensionCombination = dimensionCombination;
            this.StartTimeUtc = startTimeUtc;
            this.EndTimeUtc = endTimeUtc;
            this.SamplingTypes = samplingTypes;
            this.SeriesResolutionInMinutes = seriesResolutionInMinutes;
            this.AggregationType = aggregationType;
            this.ZeroAsNoValueSentinel = zeroAsNoValueSentinel;

            this.ValidateAndNormalize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSeriesDefinition{TId}"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="dimensionCombination">The dimension combination.</param>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="samplingTypes">The sampling types.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="aggregationType">The aggregation function used to reduce the resolution of the returned series.</param>
        /// <param name="zeroAsNoValueSentinel">
        /// if <see langword="true"/>, 0 is treated as the sentinel when the system has not received any metrics in a minutely window;
        /// if <see langword="false"/>, the sentinel is null.
        /// </param>
        public TimeSeriesDefinition(
            TId id,
            IEnumerable<KeyValuePair<string, string>> dimensionCombination,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            SamplingType[] samplingTypes,
            int seriesResolutionInMinutes = 1,
            AggregationType aggregationType = AggregationType.None,
            bool zeroAsNoValueSentinel = false)
            : this(id, dimensionCombination?.ToArray(), startTimeUtc, endTimeUtc, samplingTypes, seriesResolutionInMinutes, aggregationType, zeroAsNoValueSentinel)
        {
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        public TId Id
        {
            get
            {
                return this.id;
            }

            internal set
            {
                this.id = value;
            }
        }

        /// <summary>
        /// Gets the inclusive start time in UTC of the time range to query.
        /// </summary>
        /// <remarks>Not required for APIs that already have such a parameter.</remarks>
        public DateTime StartTimeUtc
        {
            get
            {
                return this.startTimeUtc;
            }

            internal set
            {
                this.startTimeUtc = value;
                NormalizeTime(ref this.startTimeUtc);
            }
        }

        /// <summary>
        /// Gets the inclusive end time in UTC of time range to query.
        /// </summary>
        /// <remarks>Not required for APIs that already have such a parameter.</remarks>
        public DateTime EndTimeUtc
        {
            get
            {
                return this.endTimeUtc;
            }

            internal set
            {
                this.endTimeUtc = value;
                NormalizeTime(ref this.endTimeUtc);
            }
        }

        /// <summary>
        /// Gets the requested sampling types.
        /// </summary>
        /// <remarks>Not required for APIs that already have a sampling type parameter.</remarks>
        public SamplingType[] SamplingTypes { get; internal set; }

        /// <summary>
        /// Gets the resolution window used to reduce the resolution of the returned series.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(1)]
        public int SeriesResolutionInMinutes { get; internal set; }

        /// <summary>
        /// Gets the aggregation function used to reduce the resolution of the returned series.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(AggregationType.None)]
        public AggregationType AggregationType { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use zero (or null) as the no value sentinel.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ZeroAsNoValueSentinel { get; set; }

        /// <summary>
        /// Gets the dimension combination.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, string>> DimensionCombination
        {
            get
            {
                // TODO: make wrapper allowing direct access to deserialized representation when getting results back from server case
                return this.dimensionCombination;
            }

            internal set
            {
                this.dimensionCombination = (KeyValuePair<string, string>[])value;
            }
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// Normalizes the time to minute boundary.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        private static void NormalizeTime(ref DateTime timestamp)
        {
            timestamp = new DateTime(timestamp.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
        }

        /// <summary>
        /// Validates and normalizes parameters.
        /// </summary>
        private void ValidateAndNormalize()
        {
            if (this.StartTimeUtc > this.EndTimeUtc)
            {
                throw new ArgumentException($"startTimeUtc [{this.StartTimeUtc}] must be <= endTimeUtc [{this.EndTimeUtc}]");
            }

            if (this.SamplingTypes == null || this.SamplingTypes.Length == 0)
            {
                throw new ArgumentException("samplingTypes cannot be null or empty");
            }

            if (this.SeriesResolutionInMinutes < 1)
            {
                throw new ArgumentException($"seriesResolutionInMinutes must be >= 1");
            }

            NormalizeTime(ref this.startTimeUtc);
            NormalizeTime(ref this.endTimeUtc);
        }
    }
}