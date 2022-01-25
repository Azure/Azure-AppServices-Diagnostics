using System;
using System.Security.Cryptography.X509Certificates;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Mdm client factory.
    /// </summary>
    internal static class MdmClientFactory
    {
        internal static IMdmClient GetMdmClient(IMdmDataProviderConfiguration config, X509Certificate2 certificate, string requestId)
        {
            if (config.MonitoringAccount != null && config.MonitoringAccount.StartsWith("Mock", StringComparison.OrdinalIgnoreCase))
            {
                return new MockMdmClient();
            }

            return new MdmClient(config.Endpoint, certificate, requestId);
        }
    }
}
