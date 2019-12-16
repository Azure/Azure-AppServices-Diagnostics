// <copyright file="NetworkingMdmDataProviderConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    /// <summary>
    /// Mdm data provider configuration.
    /// </summary>
    [DataSourceConfiguration(@"Mdm\Networking")]
    public class NetworkingMdmDataProviderConfiguration : IDataProviderConfiguration, IMdmDataProviderConfiguration
    {
        /// <summary>
        /// Gets or sets the base endpoint.
        /// </summary>
        [ConfigurationName("MdmNetworkingEndpoint")]
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint.
        /// </summary>
        [ConfigurationName("MdmNetworkingRegistrationCertThumbprint")]
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// Post initialize.
        /// </summary>
        public void PostInitialize()
        {
        }
    }
}
