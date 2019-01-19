using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders
{
    internal class ProductionGlobalEnvironmentResolver
    {
        private static readonly string[] PotentialProductionGlobalEnvironments =
        {
            "global.metrics.nsatc.net",
            "global.metrics.trafficmanager.net",
        };

        private static string globalStampHostName;

        /// <summary>
        /// Gets the global stamp host name.
        /// </summary>
        /// <returns>The global stamp host name.</returns>
        public static string ResolveGlobalStampHostName()
        {
            if (globalStampHostName != null)
            {
                return globalStampHostName;
            }

            for (int i = 0; i < PotentialProductionGlobalEnvironments.Length; i++)
            {
                var resolvedIp = ConnectionInfo.ResolveIp(PotentialProductionGlobalEnvironments[i], throwOnFailure: false).Result;
                if (resolvedIp != null)
                {
                    globalStampHostName = PotentialProductionGlobalEnvironments[i];
                    return PotentialProductionGlobalEnvironments[i];
                }

            }

            string errorMsg = $"ProductionGlobalEnvironmentResolver - None of the host names can be resolved: {JsonConvert.SerializeObject(PotentialProductionGlobalEnvironments)}.";

            throw new MetricsClientException(errorMsg);
        }
    }
}
