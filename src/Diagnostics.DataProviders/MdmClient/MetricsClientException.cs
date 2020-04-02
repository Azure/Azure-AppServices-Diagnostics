using System;
using System.Net;

namespace Diagnostics.DataProviders
{/// <summary>
 /// A common exception thrown by the API code after passing API parameter validation.
 /// </summary>
    [Serializable]
    public sealed class MetricsClientException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsClientException"/> class.
        /// </summary>
        /// <param name="message">Message describing exception situation.</param>
        public MetricsClientException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsClientException"/> class.
        /// </summary>
        /// <param name="message">Message describing exception situation.</param>
        /// <param name="innerException">Inner exception which caused exception situation.</param>
        public MetricsClientException(string message, Exception innerException)
            : this(message, innerException, Guid.Empty, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsClientException"/> class.
        /// </summary>
        /// <param name="message">Message describing exception situation.</param>
        /// <param name="innerException">Inner exception which caused exception situation.</param>
        /// <param name="traceId">The trace identifier used to assist in tracking what occurred.</param>
        /// <param name="responseStatusCode">The response status code, if applicable, that was received from the server.</param>
        public MetricsClientException(string message, Exception innerException, Guid traceId, HttpStatusCode? responseStatusCode)
            : base(message, innerException)
        {
            this.TraceId = traceId;
            this.ResponseStatusCode = responseStatusCode;
        }

        /// <summary>
        /// Gets the trace identifier used to assist in tracking what occurred.
        /// </summary>
        public Guid TraceId { get; private set; }

        /// <summary>
        /// Gets the response status code, if applicable, that was received from the server.
        /// </summary>
        public HttpStatusCode? ResponseStatusCode { get; private set; }
    }
}
