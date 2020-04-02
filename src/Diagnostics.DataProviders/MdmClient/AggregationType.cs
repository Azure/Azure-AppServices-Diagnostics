namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Aggregation types available when reducing resolution.
    /// </summary>
    public enum AggregationType
    {
        /// <summary>
        /// The average for the data points in the resolution window, ignore values of nulls.
        /// </summary>
        None = 0,

        /// <summary>
        /// The minimum for the data points in the resolution window, ignore values of nulls.
        /// </summary>
        Min,

        /// <summary>
        /// The maximum for the data points in the resolution window, ignore values of nulls.
        /// </summary>
        Max,

        /// <summary>
        /// The sum for the data points in the resolution window, ignore values of nulls.
        /// </summary>
        Sum,

        /// <summary>
        /// The number of the data points in the resolution window, ignore values of nulls.
        /// </summary>
        Count,

        /// <summary>
        /// The cumulative/sum value since the requested start time, ignore values of nulls.
        /// </summary>
        Cumulative,

        /// <summary>
        /// The delta of sum value as compared with the prior resolution window, ignore values of nulls.
        /// </summary>
        Delta,

        /// <summary>
        /// The automatically inferred aggregation type based on the sampling type, that is,
        /// for "Count" and "Sum" sampling types, the aggregation type is <see cref="Sum"/>,
        /// for the "Min" sampling type, the aggregation type is <see cref="Min"/>,
        /// for the "Max" sampling type, the aggregation type is <see cref="Max"/>,
        /// for percentile sampling types, the aggregation type is <see cref="None"/> or average,
        /// and for computed sampling types and composite metrics, the above inference rules were applied to the raw sampling types recursively.
        /// </summary>
        Automatic,

        /// <summary>
        /// The change in time series values over change in time.
        /// </summary>
        Rate,
    }
}
