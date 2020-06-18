using System.Collections.Generic;

namespace Diagnostics.RuntimeHost.Models
{
    /// <summary>
    /// Detector Metadata.
    /// </summary>
    public class DetectorMetadata
    {
        /// <summary>
        /// Gets or sets the code string.
        /// </summary>
        public Dictionary<string, string> ResourceFilter { get; set; }

        /// <summary>
        /// Gets or sets the metadata.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the detector name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the authors.
        /// </summary>
        public string Author { get; set; }
    }

    /// <summary>
    /// Internal Event Body
    /// </summary>
    public class InternalEventBody
    {
        /// <summary>
        /// Gets or sets EventType
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets EventContent
        /// </summary>
        public string EventContent { get; set; }
    }

    /// <summary>
    /// Internal API Insights.
    /// </summary>
    public class InternalAPIInsights : InternalAPIEvent
    {
        /// <summary>
        /// Gets or sets Message.
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Internal API Training Summary.
    /// </summary>
    public class InternalAPITrainingSummary : InternalAPITrainingEvent
    {
        /// <summary>
        /// Gets or sets LatencyInMilliseconds.
        /// </summary>
        public long LatencyInMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets StartTime.
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or sets EndTime.
        /// </summary>
        public string EndTime { get; set; }

        /// <summary>
        /// Gets or sets Content.
        /// </summary>
        public string Content { get; set; }
    }

    /// <summary>
    /// Internal API Summary.
    /// </summary>
    public class InternalAPISummary : InternalAPIEvent
    {
        /// <summary>
        /// Gets or sets OperationName.
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// Gets or sets StatusCode.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets LatencyInMilliseconds.
        /// </summary>
        public long LatencyInMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets StartTime.
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or sets EndTime.
        /// </summary>
        public string EndTime { get; set; }

        /// <summary>
        /// Gets or sets Content.
        /// </summary>
        public string Content { get; set; }
    }

    /// <summary>
    /// Internal API Exception.
    /// </summary>
    public class InternalAPIException : InternalAPIEvent
    {
        /// <summary>
        /// Gets or sets Exception Type.
        /// </summary>
        public string ExceptionType { get; set; }

        /// <summary>
        /// Gets or sets Exception Details.
        /// </summary>
        public string ExceptionDetails { get; set; }
    }

    /// <summary>
    /// Internal API Training Exception.
    /// </summary>
    public class InternalAPITrainingException : InternalAPITrainingEvent
    {
        /// <summary>
        /// Gets or sets Exception Type.
        /// </summary>
        public string ExceptionType { get; set; }

        /// <summary>
        /// Gets or sets Exception Details.
        /// </summary>
        public string ExceptionDetails { get; set; }
    }

    /// <summary>
    /// Internal API event
    /// </summary>
    public class InternalAPIEvent
    {
        /// <summary>
        /// Gets or sets Request Id.
        /// </summary>
        public string RequestId { get; set; }
    }

    /// <summary>
    /// Internal API Training Event
    /// </summary>
    public class InternalAPITrainingEvent
    {
        /// <summary>
        /// Gets or sets RequestId.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Gets or sets TrainingId..
        /// </summary>
        public string TrainingId { get; set; }

        /// <summary>
        /// Gets or sets TrainingId.
        /// </summary>
        public string ProductId { get; set; }
    }

    /// <summary>
    /// Model to receive training config.
    /// </summary>
    public class TrainingConfigModel
    {
        /// <summary>
        /// Gets or sets the training config string.
        /// </summary>
        public string TrainingConfig { get; set; }
    }
}
