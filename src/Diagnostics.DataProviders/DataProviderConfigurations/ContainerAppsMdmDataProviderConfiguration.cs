// <copyright file="ContainerAppsMdmDataProviderConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    /// <summary>
    /// Mdm data provider configuration.
    /// </summary>
    [DataSourceConfiguration(@"ContainerAppsMdm")]
    public class ContainerAppsMdmDataProviderConfiguration : DataProviderConfigurationBase, IMdmDataProviderConfiguration
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
        /// Post initialize.
        /// </summary>
        public override void PostInitialize()
        {
        }
    }
}
