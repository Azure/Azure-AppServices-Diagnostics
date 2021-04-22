using System.ComponentModel.DataAnnotations;

namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    [DataSourceConfiguration("K8SELogAnalytics")]
    public class K8SELogAnalyticsDataProviderConfiguration : LogAnalyticsDataProviderConfiguration
    {
        /// <summary>
        /// Client Id
        /// </summary>
        [ConfigurationName("ClientId")]
        [Required]
        public string ClientId { get; set; }

        /// <summary>
        /// WorkspaceId
        /// </summary>
        [ConfigurationName("WorkspaceId")]
        [Required]
        public string WorkspaceId { get; set; }

        /// <summary>
        /// Client Secret
        /// </summary>
        [ConfigurationName("ClientSecret")]
        [Required]
        public string ClientSecret { get; set; }

        /// <summary>
        /// Domain
        /// </summary>
        [ConfigurationName("Domain")]
        [Required]
        public string Domain { get; set; }

        /// <summary>
        /// Auth Endpoint
        /// </summary>
        [ConfigurationName("AuthEndpoint")]
        [Required]
        public string AuthEndpoint { get; set; }
    }
}
