namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    [DataSourceConfiguration("AzureSupportCenter")]
    public class AscDataProviderConfiguration : IDataProviderConfiguration
    {
        /// <summary>
        /// Base address for zure Support Center api endpoint.
        /// </summary>
        [ConfigurationName("BaseUri")]
        public string BaseUri { get; set; }

        /// <summary>
        /// Api endpoint for Azure Support Center api endpoint.
        /// </summary>
        [ConfigurationName("ApiUri")]
        public string ApiUri { get; set; }

        /// <summary>
        /// Api version for Azure Support Center api endpoint.
        /// </summary>
        [ConfigurationName("ApiVersion")]
        public string ApiVersion { get; set; }

        /// <summary>
        /// User Agent value passed to Azure Support Center while querying for an ADS insight.
        /// </summary>
        [ConfigurationName("UserAgent")]
        public string UserAgent { get; set; }

        /// <summary>
        ///  AAD Tenant to authenticate with.
        /// </summary>
        [ConfigurationName("AADAuthority")]
        public string AADAuthority { get; set; }

        /// <summary>
        ///  Resource to issue token for.
        /// </summary>
        [ConfigurationName("TokenResource")]
        public string TokenResource { get; set; }

        /// <summary>
        ///  Client id of Applens trusted by Azure Support Center.
        /// </summary>
        [ConfigurationName("ClientId")]
        public string ClientId { get; set; }

        /// <summary>
        ///  App Key generated to authentication with Azure Support Center.
        /// </summary>
        [ConfigurationName("AppKey")]
        public string AppKey { get; set; }

        public void PostInitialize()
        {
        }
    }
}
