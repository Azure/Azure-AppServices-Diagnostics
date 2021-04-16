using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders
{
    internal static class LogAnalyticsClientFactory
    {
        internal static ILogAnalyticsClient GetLogAnalyticsClient(LogAnalyticsDataProviderConfiguration config, string requestId)
        {
            return config.Provider switch
            {
                "K8SE" => new K8SELogAnalyticsClient(requestId),
                "Mock" => new MockLogAnalyticsClient(),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
