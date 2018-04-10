using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Operation Context
    /// </summary>
    public class OperationContext
    {
        /// <summary>
        /// Site Resource
        /// </summary>
        public SiteResource Resource;

        /// <summary>
        /// Start Time(UTC) for data measurement
        /// </summary>
        public string StartTime;

        /// <summary>
        /// End Tim(UTC) for data measurement
        /// </summary>
        public string EndTime;

        /// <summary>
        /// TimeGrain in minutes for aggregating data.
        /// </summary>
        public string TimeGrain;

        public OperationContext(SiteResource resource, string startTimeStr, string endTimeStr, string timeGrain = "5")
        {
            Resource = resource;
            StartTime = startTimeStr;
            EndTime = endTimeStr;
            TimeGrain = timeGrain;
        }
    }
}
