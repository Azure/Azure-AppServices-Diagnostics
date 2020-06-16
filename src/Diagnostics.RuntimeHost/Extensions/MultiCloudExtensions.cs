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
    }
}
