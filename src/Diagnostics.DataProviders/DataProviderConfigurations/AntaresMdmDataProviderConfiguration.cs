// <copyright file="AntaresMdmDataProviderConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    /// <summary>
    /// Mdm data provider configuration.
    /// </summary>
    [DataSourceConfiguration(@"Mdm")]
    public class AntaresMdmDataProviderConfiguration : IDataProviderConfiguration
    {
        /// <summary>
        /// Gets or sets the base endpoint.
        /// </summary>
        [ConfigurationName("MdmShoeboxEndpoint")]
        public string Endpoint { get; set; }
        
        /// <summary>
        /// Gets or sets monitoring account.
        /// </summary>
        [ConfigurationName("MdmShoeboxAccount")]
        public string MonitoringAccount { get; set; }

        /// <summary>
        /// Name of certificate in prod key vault.
        /// </summary>
        [ConfigurationName("CertificateName")]
        public string CertificateName { get; set; }

        /// <summary>
        /// Post initialize.
        /// </summary>
        public void PostInitialize()
        {
        }
    }
}
