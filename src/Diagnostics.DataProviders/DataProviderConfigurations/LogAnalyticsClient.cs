using Diagnostics.DataProviders.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    public class LogAnalyticsClient :ILogAnalyticsClient
    {
        private K8SELogAnalyticsDataProviderConfiguration _configuration;
        public LogAnalyticsClient(K8SELogAnalyticsDataProviderConfiguration configuration)
        {
            _configuration = configuration;
        }
    }
}
