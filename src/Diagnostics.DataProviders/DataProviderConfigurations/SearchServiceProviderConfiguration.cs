using Diagnostics.DataProviders;
namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    [DataSourceConfiguration("SearchAPI")]
    public class SearchServiceProviderConfiguration : DataProviderConfigurationBase, IDataProviderConfiguration
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

        /// <summary>
        /// UseCertAuth.
        /// </summary>
        [ConfigurationName("UseCertAuth")]
        public bool UseCertAuth { get; set; }

        /// <summary>
        /// CertThumbprint.
        /// </summary>
        [ConfigurationName("CertThumbprint")]
        public string CertThumbprint { get; set; }

        /// <summary>
        /// CertSubjectName.
        /// </summary>
        [ConfigurationName("CertSubjectName")]
        public string CertSubjectName { get; set; }
    }
}
