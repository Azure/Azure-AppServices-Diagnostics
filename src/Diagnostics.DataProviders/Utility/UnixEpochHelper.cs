using System;

namespace Diagnostics.DataProviders.Utility
{
    /// <summary>
    /// Methods and values related to time.
    /// </summary>
    /// <remarks>
    /// The formal definition of unix time is the # of seconds that have elapsed since UTC
    /// DateTime(1970, 1, 1, 0, 0, 0). The metrics system refers to unix time as the number of milliseconds
    /// that have elapsed since UTC DateTime(1970, 1, 1, 0, 0, 0).
    /// </remarks>
    internal static class UnixEpochHelper
    {
        /// <summary>
        /// The number of ticks in a millisecond.
        /// </summary>
        internal const long TicksPerMillisecond = 10000;

        /// <summary>
        /// The Unix epoch (January 1, 1970)
        /// </summary>
        internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0);

        /// <summary>
        /// The milliseconds between the Unix epoch (January 1, 1970) and the Microsoft epoch (January 1, 0001).
        /// </summary>
        private static readonly long UnixEpochMsEpochDeltaMillis = UnixEpoch.Ticks / TicksPerMillisecond;

        /// <summary>
        /// Gets the Unix time for the given <paramref name="utcTime"/>.
        /// </summary>
        /// <param name="utcTime">A given UTC time</param>
        /// <returns>The Unix time for the given <paramref name="utcTime"/></returns>
        internal static long GetMillis(DateTime utcTime)
        {
            return (utcTime.Ticks / TicksPerMillisecond) - UnixEpochMsEpochDeltaMillis;
        }
    }
}
