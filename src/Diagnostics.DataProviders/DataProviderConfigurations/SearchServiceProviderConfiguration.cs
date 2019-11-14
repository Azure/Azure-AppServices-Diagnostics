using Diagnostics.DataProviders;
namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    [DataSourceConfiguration("SearchAPI")]
    public class SearchServiceProviderConfiguration : IDataProviderConfiguration
    {
        /// <summary>
        /// SearchAPIEnabled
        /// </summary>
        [ConfigurationName("SearchAPIEnabled")]
        public bool SearchAPIEnabled { get; set; }
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
        public string AADResource { get; set; }

        /// <summary>
        /// SearchAPI endpoint.
        /// </summary>
        [ConfigurationName("SearchEndpoint")]
        public string SearchEndpoint { get; set; }

        /// <summary>
        /// TrainingAPI endpoint.
        /// </summary>
        [ConfigurationName("TrainingEndpoint")]
        public string TrainingEndpoint { get; set; }

        public void PostInitialize()
        {
        }
    }
}
