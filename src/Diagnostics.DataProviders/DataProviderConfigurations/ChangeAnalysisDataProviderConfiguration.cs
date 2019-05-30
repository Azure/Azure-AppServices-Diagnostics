namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    [DataSourceConfiguration("ChangeAnalysis")]
    public class ChangeAnalysisDataProviderConfiguration : IDataProviderConfiguration
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
        [ConfigurationName("AADResource")]
        public string AADChangeAnalysisResource { get; set; }

        /// <summary>
        /// API endpoint.
        /// </summary>
        [ConfigurationName("Endpoint")]
        public string Endpoint { get; set; }

        /// <summary>
        /// API version.
        /// </summary>
        [ConfigurationName("Apiversion")]
        public string Apiversion { get; set; }

        public void PostInitialize()
        {
        }
    }
}
