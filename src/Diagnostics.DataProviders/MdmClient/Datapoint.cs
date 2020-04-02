using System;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// A class representing a datapoint of time-value pair.
    /// </summary>
    /// <typeparam name="T">The type for the datapoint value.</typeparam>
    public struct Datapoint<T>
    {
        /// <summary>
        /// The timestamp in UTC.
        /// </summary>
        private readonly DateTime timestampUtc;

        /// <summary>
        /// The value of the datapoint.
        /// </summary>
        private readonly T value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Datapoint{T}"/> struct.
        /// </summary>
        /// <param name="timestampUtc">The timestamp UTC.</param>
        /// <param name="value">The value.</param>
        public Datapoint(DateTime timestampUtc, T value)
        {
            this.timestampUtc = timestampUtc;
            this.value = value;
        }

        /// <summary>
        /// Gets the timestamp UTC.
        /// </summary>
        public DateTime TimestampUtc
        {
            get
            {
                return this.timestampUtc;
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public T Value
        {
            get
            {
                return this.value;
            }
        }
    }
}
