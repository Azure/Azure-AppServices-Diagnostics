﻿using System;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Mdm client factory.
    /// </summary>
    internal static class MdmClientFactory
    {
        internal static IMdmClient GetMdmClient(IMdmDataProviderConfiguration config, string requestId)
        {
            if (config.MonitoringAccount != null && config.MonitoringAccount.StartsWith("Mock", StringComparison.OrdinalIgnoreCase))
            {
                return new MockMdmClient();
            }

            return new MdmClient(config, requestId);
        }
    }
}
