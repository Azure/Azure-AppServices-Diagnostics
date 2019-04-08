using System;
namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    [DataSourceConfiguration("ChangeAnalysis")]
    public class ChangeAnalysisDataProviderConfiguration: IDataProviderConfiguration
    {
        /// <summary>
        /// Client Id.
        /// </summary>
        [ConfigurationName("ClientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// App Key.
        /// </summary>
        [ConfigurationName("AppKey")]
        public string AppKey { get; set; }

        /// <summary>
        /// Tenant to authenticate with.
        /// </summary>
        [ConfigurationName("AADAuthority")]
        public string AADAuthority { get; set; }

        /// <summary>
        /// Resource to issue token.
        /// </summary>
        [ConfigurationName("AADChangeAnalysisResource")]
        public string AADChangeAnalysisResource { get; set; }

        public void PostInitialize()
        {

        }
    }
}
