using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils
{
    public class OperationContext
    {
        public SiteResource Resource;

        public string StartTime;

        public string EndTime;

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
