// <copyright file="GenericMdmDataProviderConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Security.Cryptography.X509Certificates;

namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    /// <summary>
    /// Mdm data provider configuration.
    /// </summary>
    public class GenericMdmDataProviderConfiguration : IDataProviderConfiguration
    {
        /// <summary>
        /// Gets or sets the base endpoint.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the certificate name.
        /// </summary>
        public string CertificateName { get; set; }

        /// <summary>
        /// Gets or sets monitoring account.
        /// </summary>
        public string MonitoringAccount { get; set; }

        /// <summary>
        /// Post initialize.
        /// </summary>
        public void PostInitialize()
        {
        }
    }

    public class GenericMdmDataProviderConfigurationWrapper: IDataProviderConfiguration, IMdmDataProviderConfiguration
    {
        /// <summary>
        /// Gets or sets the base endpoint.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint.
        /// </summary>
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// Gets or sets monitoring account.
        /// </summary>
        public string MonitoringAccount { get; set; }

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
                throw new ArgumentNullException("Configuration is null.");
            }
            if (config.MonitoringAccount != null && config.MonitoringAccount.StartsWith("Mock"))
            {
                CertificateThumbprint = "BAD0BAD0BAD0BAD0BAD0BAD0BAD0BAD0BAD0BAD0";
            }
            else
            {
                using (X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
                using (X509Store userStore = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    userStore.Open(OpenFlags.ReadOnly);

                    var certificateCollection = store.Certificates;
                    certificateCollection.AddRange(userStore.Certificates);
                    var certificates = certificateCollection.Find(X509FindType.FindBySubjectName, config.CertificateName, true);

                    if (certificates.Count == 0)
                    {
                        throw new SystemException("Certificate is not found.");
                    }

                    CertificateThumbprint = certificates[0].Thumbprint;
                }
            }

            Endpoint = config.Endpoint;
            MonitoringAccount = config.MonitoringAccount;
        }
    }
}
