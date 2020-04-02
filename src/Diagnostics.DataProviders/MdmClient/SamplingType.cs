using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// The sampling types.
    /// </summary>
    public struct SamplingType : IEquatable<SamplingType>
    {
        /// <summary>
        /// The built-in sampling types.
        /// </summary>
        internal static readonly Dictionary<string, SamplingType> BuiltInSamplingTypes;

        // The pre-defined sampling types.
        private static readonly SamplingType CountSamplingType = new SamplingType("Count");

        private static readonly SamplingType SumSamplingType = new SamplingType("Sum");
        private static readonly SamplingType MinSamplingType = new SamplingType("Min");
        private static readonly SamplingType MaxSamplingType = new SamplingType("Max");
        private static readonly SamplingType AverageSamplingType = new SamplingType("Average");
        private static readonly SamplingType NullableAverageSamplingType = new SamplingType("NullableAverage");
        private static readonly SamplingType RateSamplingType = new SamplingType("Rate");
        private static readonly SamplingType Percentile50thSamplingType = new SamplingType("50th percentile");
        private static readonly SamplingType Percentile90thSamplingType = new SamplingType("90th percentile");
        private static readonly SamplingType Percentile95thSamplingType = new SamplingType("95th percentile");
        private static readonly SamplingType Percentile99thSamplingType = new SamplingType("99th percentile");
        private static readonly SamplingType Percentile999thSamplingType = new SamplingType("99.9th percentile");

        /// <summary>
        /// the sampling type as a string.
        /// </summary>
        private readonly string name;

        /// <summary>
        /// The hashcode computed once on initialization.
        /// </summary>
        private readonly int hashcode;

        /// <summary>
        /// Initializes static members of the <see cref="SamplingType"/> struct.
        /// </summary>
        static SamplingType()
        {
            BuiltInSamplingTypes = new Dictionary<string, SamplingType>(StringComparer.OrdinalIgnoreCase)
            {
                { Count.ToString(), Count },
                { Sum.ToString(), Sum },
                { Min.ToString(), Min },
                { Max.ToString(), Max },
                { Average.ToString(), Average },
                { NullableAverage.ToString(), NullableAverage },
                { Rate.ToString(), Rate },
                { Percentile50th.ToString(), Percentile50th },
                { Percentile90th.ToString(), Percentile90th },
                { Percentile95th.ToString(), Percentile95th },
                { Percentile99th.ToString(), Percentile99th },
                { Percentile999th.ToString(), Percentile999th },
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SamplingType" /> struct.
        /// </summary>
        /// <param name="name">The sampling type as a string.</param>
        [JsonConstructor]
        public SamplingType(string name)
            : this()
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Must not be null or white spaces", nameof(name));
            }

            this.name = name;
            this.hashcode = StringComparer.OrdinalIgnoreCase.GetHashCode(name);
        }

        /// <summary>
        /// Gets the Count sampling type.
        /// </summary>
        public static SamplingType Count
        {
            get
            {
                return CountSamplingType;
            }
        }

        /// <summary>
        /// Gets the Sum sampling type.
        /// </summary>
        public static SamplingType Sum
        {
            get
            {
                return SumSamplingType;
            }
        }

        /// <summary>
        /// Gets the Min sampling type.
        /// </summary>
        public static SamplingType Min
        {
            get
            {
                return MinSamplingType;
            }
        }

        /// <summary>
        /// Gets the Max sampling type.
        /// </summary>
        public static SamplingType Max
        {
            get
            {
                return MaxSamplingType;
            }
        }

        /// <summary>
        /// Gets the "Average" sampling type.
        /// </summary>
        public static SamplingType Average
        {
            get
            {
                return AverageSamplingType;
            }
        }

        /// <summary>
        /// Gets the "NullableAverage" sampling type.
        /// </summary>
        public static SamplingType NullableAverage
        {
            get
            {
                return NullableAverageSamplingType;
            }
        }

        /// <summary>
        /// Gets the "Rate" (divided by 60 seconds) sampling type.
        /// </summary>
        public static SamplingType Rate
        {
            get
            {
                return RateSamplingType;
            }
        }

        /// <summary>
        /// Gets the 50th percentile sampling type.
        /// </summary>
        public static SamplingType Percentile50th
        {
            get
            {
                return Percentile50thSamplingType;
            }
        }

        /// <summary>
        /// Gets the 90th percentile sampling type.
        /// </summary>
        public static SamplingType Percentile90th
        {
            get
            {
                return Percentile90thSamplingType;
            }
        }

        /// <summary>
        /// Gets the 95th percentile sampling type.
        /// </summary>
        public static SamplingType Percentile95th
        {
            get
            {
                return Percentile95thSamplingType;
            }
        }

        /// <summary>
        /// Gets the 99th percentile sampling type.
        /// </summary>
        public static SamplingType Percentile99th
        {
            get
            {
                return Percentile99thSamplingType;
            }
        }

        /// <summary>
        /// Gets the 99.9th percentile sampling type.
        /// </summary>
        public static SamplingType Percentile999th
        {
            get
            {
                return Percentile999thSamplingType;
            }
        }

        /// <summary>
        /// the sampling type as a string.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }
        }

        /// <summary>
        /// Creates distinct count sampling type.
        /// </summary>
        /// <param name="distinctCountDimensionName">The distinct count dimension name</param>
        /// <returns>Sampling type corrosponding to the given distinct count dimension name.</returns>
        public static SamplingType CreateDistinctCountSamplingType(string distinctCountDimensionName)
        {
            return new SamplingType("DistinctCount_" + distinctCountDimensionName);
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.name;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(SamplingType other)
        {
            return string.Equals(this.name, other.name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return this.hashcode;
        }
    }
}
