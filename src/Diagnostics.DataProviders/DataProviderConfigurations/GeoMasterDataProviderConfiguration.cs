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
    }
}
