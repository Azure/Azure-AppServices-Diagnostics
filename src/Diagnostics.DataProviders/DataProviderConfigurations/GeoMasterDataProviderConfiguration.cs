namespace Diagnostics.DataProviders
{
    [DataSourceConfiguration("GeoMaster")]
    public class GeoMasterDataProviderConfiguration : DataProviderConfigurationBase, IDataProviderConfiguration
    {
        public GeoMasterDataProviderConfiguration()
        {
        }

        /// <summary>
        /// GeomasterEndpoint
        /// </summary>
        [ConfigurationName("GeoEndpointAddress")]
        public string GeoEndpointAddress { get; set; }

        /// <summary>
        /// IsInternal
        /// </summary>
        [ConfigurationName("Token")]
        public string Token { get; set; }

        /// <summary>
        /// Name of certificate in Prod key vault.
        /// </summary>
        [ConfigurationName("CertificateName")]
        public string CertificateName { get; set; }

        /// <summary>
        /// Subject name of the certificate
        /// </summary>
        [ConfigurationName("GeoCertSubjectName")]
        public string GeoCertSubjectName { get; set; }
        /// <summary>
        /// Max Retry Count
        /// </summary>
        [ConfigurationName("Retry:MaxRetryCount")]
        public int MaxRetryCount { get; set; }
        /// <summary>
        /// Delay in Seconds between two retries
        /// </summary>
        [ConfigurationName("Retry:RetryDelayInSeconds")]
        public int RetryDelayInSeconds { get; set; }
    }
}
