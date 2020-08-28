using System;
using Diagnostics.DataProviders;

namespace Microsoft.Extensions.Configuration
{
    public static class MultiCloudExtensions
    {
        /// <summary>
        /// Determines if this cloud is Public Azure cloud.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static bool IsPublicAzure(this IConfiguration configuration)
        {
            return configuration.GetValue<string>("CloudDomain").Equals(DataProviderConstants.AzureCloud, StringComparison.CurrentCultureIgnoreCase)
                || configuration.GetValue<string>("CloudDomain").Equals(DataProviderConstants.AzureCloudAlternativeName, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Determines if this cloud is an airgapped cloud.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static bool IsAirGappedCloud(this IConfiguration configuration)
        {
            return configuration.GetValue<string>("CloudDomain").Equals(DataProviderConstants.AzureUSSec, StringComparison.CurrentCultureIgnoreCase)
                || configuration.GetValue<string>("CloudDomain").Equals(DataProviderConstants.AzureUSNat, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
