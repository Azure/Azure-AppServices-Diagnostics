using System.ComponentModel.DataAnnotations;

namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    [DataSourceConfiguration("ChangeAnalysis")]
    public class ChangeAnalysisDataProviderConfiguration : DataProviderConfigurationBase, IDataProviderConfiguration
    {
        /// <summary>
        /// Client Id.
        /// </summary>
        [ConfigurationName("ClientId")]
        [Required]
        public string ClientId { get; set; }

        /// <summary>
        /// App Key.
        /// </summary>
        [ConfigurationName("AppKey")]
        [Required]
        public string AppKey { get; set; }

        /// <summary>
        /// Tenant to authenticate with.
        /// </summary>
        [ConfigurationName("AADAuthority")]
        [Required]
        public string AADAuthority { get; set; }

        /// <summary>
        /// Resource to issue token.
        /// </summary>
        [ConfigurationName("AADResource")]
        [Required]
        public string AADChangeAnalysisResource { get; set; }

        /// <summary>
        /// API endpoint.
        /// </summary>
        [ConfigurationName("Endpoint")]
        [Required]
        public string Endpoint { get; set; }

        /// <summary>
        /// API version.
        /// </summary>
        [ConfigurationName("Apiversion")]
        public string Apiversion { get; set; }
    }
}
