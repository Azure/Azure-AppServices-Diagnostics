using System;
using System.Collections.Generic;
using System.Text;

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
        /// End Tim(UTC) for data measurement
        /// </summary>
        public string EndTime { get; private set; }

        /// <summary>
        /// sets to true when detector is called from external source (Azure portal, CLI ...)
        /// sets to false when detexctor is called from internal source (Applens ..)
        /// </summary>
        public bool IsExternalCall { get; private set; }

        /// <summary>
        /// TimeGrain in minutes for aggregating data.
        /// </summary>
        public string TimeGrain { get; private set; }

        public OperationContext(TResource resource, string startTimeStr, string endTimeStr, bool isExternalCall, string timeGrain = "5")
        {
            Resource = resource;
            StartTime = startTimeStr;
            EndTime = endTimeStr;
            IsExternalCall = isExternalCall;
            TimeGrain = timeGrain;
        }
    }
}
