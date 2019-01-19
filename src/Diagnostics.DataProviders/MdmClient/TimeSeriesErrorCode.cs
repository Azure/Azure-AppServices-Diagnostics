namespace Diagnostics.DataProviders
{ 
    /// <summary>
    /// Error codes to indicate how the query on the time series fails.
    /// </summary>
    /// <remarks>All error codes are negative to be compatible with the v1 serialization where -1 was used for all failures.</remarks>
    public enum TimeSeriesErrorCode
    {
        /// <summary>
        /// The success code.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The invalid series code (the time series was never emitted).
        /// </summary>
        InvalidSeries = -1,

        /// <summary>
        /// The sampling type is invalid.
        /// </summary>
        InvalidSamplingType = -2,

        /// <summary>
        /// The request entity is too large, and the typical reason is that the requested time range is too long (wide).
        /// </summary>
        RequestEntityTooLarge = -3,

        /// <summary>
        /// The request is throttled.
        /// </summary>
        Throttled = -4,

        // *** Add new error code here ***

        /// <summary>
        /// Timed out.
        /// </summary>
        TimeOut = -62,

        /// <summary>
        /// The bad request code.
        /// </summary>
        BadRequest = -63,

        /// <summary>
        /// Internal error happened.
        /// </summary>
        /// <remarks>Don't add error code smaller than -64 since we want to use a single byte to serialize the error code with variable length encoding.</remarks>
        InternalError = -64
    }
}