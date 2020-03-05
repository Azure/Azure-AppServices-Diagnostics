using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using Diagnostics.ModelsAndUtils.Utilities;
using System.Collections.Generic;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Operation Context
    /// </summary>
    public class OperationContext<TResource> where TResource : IResource
    {
        /// <summary>
        /// Resource Object
        /// See <see cref="App"/> and <see cref="HostingEnvironment"/>
        /// </summary>
        public TResource Resource { get; private set; }

        /// <summary>
        /// Start Time(UTC) for data measurement
        /// </summary>
        public string StartTime { get; private set; }

        /// <summary>
        /// End Time(UTC) for data measurement
        /// </summary>
        public string EndTime { get; private set; }

        /// <summary>
        /// sets to false when detector should run as it is being called from external source (Azure portal, CLI ...)
        /// sets to true when detector should run as it is being called from internal source (Applens ..)
        /// </summary>
        public bool IsInternalCall { get; private set; }

        /// <summary>
        /// Request Id
        /// </summary>
        public string RequestId { get; private set; }

        /// <summary>
        /// If this detector is being run in the context of a support topic, i.e. called from Azure Support Center
        /// then this value will be populated with the selected support topic
        /// </summary>
        public SupportTopic SupportTopic { get; private set; }

        /// <summary>
        /// If this detector is being called from Azure Support Center
        /// then this value will be populated with the provided parameters from ASC
        /// </summary>
        public string ASCParameters { get; private set; }

        /// <summary>
        /// TimeGrain in minutes for aggregating data.
        /// </summary>
        public string TimeGrain { get; private set; }

        /// <summary>
        /// Form object
        /// </summary>
        public Form Form { get; private set; }

        /// <summary>
        /// Get the cloud environment where this code is executing.
        /// </summary>
        public string CloudDomain { get; private set; }

        public Dictionary<string, string> QueryParams { get; set; }

        public static implicit operator OperationContext(OperationContext<TResource> context)
        {
            return new OperationContext(context.Resource, context.StartTime, context.EndTime, context.IsInternalCall,
                context.RequestId, context.TimeGrain, context.SupportTopic);
        }

        public OperationContext(TResource resource, string startTimeStr, string endTimeStr, bool isInternalCall, string requestId, string timeGrain = "5", SupportTopic supportTopic = null, Form form = null, string cloudDomain = null, string ascParams = null)
        {
            Resource = resource;
            StartTime = startTimeStr;
            EndTime = endTimeStr;
            IsInternalCall = isInternalCall;
            RequestId = requestId;
            TimeGrain = timeGrain;
            SupportTopic = supportTopic;
            Form = form;
            CloudDomain = cloudDomain;
            QueryParams = new Dictionary<string, string>();
            ASCParameters = ascParams;
        }
    }

    public class OperationContext : OperationContext<IResource>
    {
        public OperationContext(IResource resource, string startTimeStr, string endTimeStr, bool isInternalCall,
            string requestId, string timeGrain = "5", SupportTopic supportTopic = null, string ascParams = null) :
            base(resource, startTimeStr, endTimeStr, isInternalCall, requestId, timeGrain, supportTopic, null, null, ascParams)
        {
        }
    }
}
