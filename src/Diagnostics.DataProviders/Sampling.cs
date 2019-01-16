using Microsoft.Cloud.Metrics.Client.Metrics;
using System;
using System.Collections.Generic;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Sampling type
    /// </summary>
    [Flags]
    public enum Sampling
    {
        /// <summary>
        /// 999th percentile
        /// </summary>
        Percentile999th = 0x01,

        /// <summary>
        /// 99th percentile
        /// </summary>
        Percentile99th = 0x02,

        /// <summary>
        /// 95th percentile
        /// </summary>
        Percentile95th = 0x04,

        /// <summary>
        /// 90th percentile
        /// </summary>
        Percentile90th = 0x08,

        /// <summary>
        /// 50th percentile
        /// </summary>
        Percentile50th = 0x10,

        /// <summary>
        /// Rate
        /// </summary>
        Rate = 0x20,

        /// <summary>
        /// Nullable average
        /// </summary>
        NullableAverage = 0x40,

        /// <summary>
        /// Average
        /// </summary>
        Average = 0x80,

        /// <summary>
        /// Max
        /// </summary>
        Max = 0x100,

        /// <summary>
        /// Min
        /// </summary>
        Min = 0x200,

        /// <summary>
        /// Sum
        /// </summary>
        Sum = 0x400,

        /// <summary>
        /// Count
        /// </summary>
        Count = 0x800
    }

    static class SamplingExtensions
    {
        public static List<SamplingType> ToSamplingType(this Sampling sampling)
        {
            var types = new List<SamplingType>();

            if (sampling.HasFlag(Sampling.Percentile999th)) types.Add(SamplingType.Percentile999th);
            if (sampling.HasFlag(Sampling.Percentile99th)) types.Add(SamplingType.Percentile99th);
            if (sampling.HasFlag(Sampling.Percentile95th)) types.Add(SamplingType.Percentile95th);
            if (sampling.HasFlag(Sampling.Percentile90th)) types.Add(SamplingType.Percentile90th);
            if (sampling.HasFlag(Sampling.Percentile50th)) types.Add(SamplingType.Percentile50th);
            if (sampling.HasFlag(Sampling.Rate)) types.Add(SamplingType.Rate);
            if (sampling.HasFlag(Sampling.NullableAverage)) types.Add(SamplingType.NullableAverage);
            if (sampling.HasFlag(Sampling.Average)) types.Add(SamplingType.Average);
            if (sampling.HasFlag(Sampling.Max)) types.Add(SamplingType.Max);
            if (sampling.HasFlag(Sampling.Min)) types.Add(SamplingType.Min);
            if (sampling.HasFlag(Sampling.Sum)) types.Add(SamplingType.Sum);
            if (sampling.HasFlag(Sampling.Count)) types.Add(SamplingType.Count);

            return types;
        }
    }
}
