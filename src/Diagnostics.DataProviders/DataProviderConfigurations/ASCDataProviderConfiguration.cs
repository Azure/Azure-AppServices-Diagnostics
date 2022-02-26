using System.ComponentModel.DataAnnotations;

namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    [DataSourceConfiguration("AzureSupportCenter")]
    public class AscDataProviderConfiguration : DataProviderConfigurationBase, IDataProviderConfiguration
    {
        /// <summary>
        /// Base address for zure Support Center api endpoint.
        /// </summary>
        [ConfigurationName("BaseUri")]
        [Required]
        public string BaseUri { get; set; }

        /// <summary>
        /// Api endpoint for Azure Support Center api endpoint.
        /// </summary>
        [ConfigurationName("ApiUri")]
        [Required]
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
        [Required]
        public string AADAuthority { get; set; }

        /// <summary>
        ///  Resource to issue token for.
        /// </summary>
        [ConfigurationName("TokenResource")]
        [Required]
        public string TokenResource { get; set; }

        /// <summary>
        ///  Client id of Applens trusted by Azure Support Center.
        /// </summary>
        [ConfigurationName("ClientId")]
        [Required]
        public string ClientId { get; set; }

        /// <summary>
        ///  App Key generated to authentication with Azure Support Center.
        /// </summary>
        [ConfigurationName("AppKey")]
        [Required]
        public string AppKey { get; set; }

        /// Header value based on which we block calls to ASC
        /// </summary>
        [ConfigurationName("DiagAscHeader")]
        public string DiagAscHeader { get; set; }

        [ConfigurationName("TokenRequestorCertSubjectName")]
        [Required]
        public string TokenRequestorCertSubjectName { get; set; }
    }
}
