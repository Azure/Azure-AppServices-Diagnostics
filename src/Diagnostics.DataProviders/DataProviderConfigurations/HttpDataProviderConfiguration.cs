using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    [DataSourceConfiguration("HttpProvider")]
    public class HttpDataProviderConfiguration : DataProviderConfigurationBase, IDataProviderConfiguration
    {
        /// <summary>
        /// Subject name of the certificate that will be used by default to acquire token from AAD while sending HTTP requests.
        /// </summary>
        [ConfigurationName("DefaultTokenRequestorCertSubjectName")]
        [Required]
        public string DefaultTokenRequestorCertSubjectName { get; set; }

        /// <summary>
        /// Subject name of the certificate that will be sent as client certificate along with the HTTP request to support certificate based authentication.
        /// </summary>
        [ConfigurationName("DefaultClientCertAuthSubjectName")]
        [Required]
        public string DefaultClientCertAuthSubjectName { get; set; }

        /// <summary>
        /// User Agent value passed to external endpoint.
        /// </summary>
        [ConfigurationName("UserAgent")]
        [Required]
        public string UserAgent { get; set; }

        /// <summary>
        /// Domain URI of the AAD Tenant where the aad app resides.
        /// </summary>
        [ConfigurationName("DefaultAADAuthority")]
        [Required]
        public string DefaultAADAuthority { get; set; }

        private Uri _defaultAADAuthorityUri = default(Uri);

        public Uri DefaultAADAuthorityUri
        { 
            get {
                if (_defaultAADAuthorityUri == null)
                {
                    _defaultAADAuthorityUri = new Uri(DefaultAADAuthority);
                }                
                return _defaultAADAuthorityUri;
            } 
        }

        /// <summary>
        ///  Client id of of the aad app to request the token from.
        /// </summary>
        [ConfigurationName("DefaultAADClientId")]
        [Required]
        public string DefaultAADClientId { get; set; }

        /// <summary>
        ///  Timeout value in milliseconds for all outbound requests.
        /// </summary>
        [ConfigurationName("DefaultRequestTimeOutInMilliSeconds")]
        [Required]
        public int DefaultRequestTimeOutInMilliSeconds { get; set; }

        /// <summary>
        /// Number of connections that are open simultaneously to a given destination URL.
        /// </summary>
        [ConfigurationName("MaxConnectionsPerServer")]
        [Required]
        public int MaxConnectionsPerServer { get; set; }

        /// <summary>
        /// Comma seperated list of headers that are prohibited to include in outgoiung HTTP calls
        /// </summary>
        [ConfigurationName("ProhibitedHeadersCSV")]
        [Required]
        public string ProhibitedHeaders { get; set; }

        private List<string> _prohibitedHeadersList = new List<string>();
        public List<string> ProhibitedHeadersList 
        { 
            get 
            {
                if (_prohibitedHeadersList.Count < 1 && !string.IsNullOrWhiteSpace(ProhibitedHeaders))
                {
                    foreach (string currHeaderName in ProhibitedHeaders.Split(','))
                    {
                        if (!string.IsNullOrWhiteSpace(currHeaderName))
                        {
                            _prohibitedHeadersList.Add(currHeaderName.Trim());
                        }
                    }
                }
                return _prohibitedHeadersList;
            } 
        }
    }
}
