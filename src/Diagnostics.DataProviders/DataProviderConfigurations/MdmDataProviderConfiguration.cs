namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    /// <summary>
    /// Mdm data provider configuration
    /// </summary>
    [DataSourceConfiguration("Mdm")]
    public class MdmDataProviderConfiguration : IDataProviderConfiguration
    {
        /// <summary>
        /// Gets or sets the base endpoint
        /// </summary>
        [ConfigurationName("Endpoint")]
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint
        /// </summary>
        [ConfigurationName("CertificateThumbprint")]
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// Gets or sets monitoring account
        /// </summary>
        [ConfigurationName("MonitoringAccount")]
        public string MonitoringAccount { get; set; }

        /// <summary>
        /// Post initialize.
        /// </summary>
        public void PostInitialize()
        {
        }
    }
}
