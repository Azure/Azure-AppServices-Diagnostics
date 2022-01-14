// <copyright file="GenericMdmDataProviderConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;

namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    /// <summary>
    /// Mdm data provider configuration.
    /// </summary>
    public class GenericMdmDataProviderConfiguration : DataProviderConfigurationBase, IMdmDataProviderConfiguration
    {
        /// <summary>
        /// Gets or sets the base endpoint.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets monitoring account.
        /// </summary>
        public string MonitoringAccount { get; set; }

        /// <summary>
        /// Gets or sets certificate name.
        /// </summary>
        public string CertificateSubjectName { get; set; }

        /// <summary>
        /// Post initialize.
        /// </summary>
        public void PostInitialize()
        {
        }
    }

    public class GenericMdmDataProviderConfigurationWrapper : DataProviderConfigurationBase, IDataProviderConfiguration, IMdmDataProviderConfiguration
    {
        /// <summary>
        /// Gets or sets the base endpoint.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets monitoring account.
        /// </summary>
        public string MonitoringAccount { get; set; }

        /// <summary>
        /// Gets or sets certificate name.
        /// </summary>
        public string CertificateName { get; set; }

        /// <summary>
        /// Post initialize.
        /// </summary>
        public void PostInitialize()
        {
        }

        public GenericMdmDataProviderConfigurationWrapper(GenericMdmDataProviderConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("Supplied MDM configuration is null.");
            }
            Endpoint = config.Endpoint;
            MonitoringAccount = config.MonitoringAccount;
        }
    }
}
