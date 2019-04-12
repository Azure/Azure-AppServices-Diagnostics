// <copyright file="SupportObserverDataProviderConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Diagnostics.DataProviders
{
    [DataSourceConfiguration("SupportObserver")]
    public class SupportObserverDataProviderConfiguration : IDataProviderConfiguration
    {
        public SupportObserverDataProviderConfiguration()
        {
        }

        /// <summary>
        /// Observer endpoint.
        /// </summary>
        [ConfigurationName("Endpoint")]
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets client Id.
        /// </summary>
        [ConfigurationName("ClientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets app key.
        /// </summary>
        [ConfigurationName("AppKey")]
        public string AppKey { get; set; }

        [ConfigurationName("IsMockConfigured", DefaultValue = false)]
        public bool IsMockConfigured { get; set; }

        /// <summary>
        /// Gets or sets tenant to authenticate with.
        /// </summary>
        [ConfigurationName("AADAuthority")]
        public string AADAuthority { get; set; }

        /// <summary>
        /// Gets resourceId for WAWSObserver AAD app.
        /// </summary>
        public string WawsObserverResourceId
        {
            get { return "d1abfd91-e19c-426e-802f-a6c55421a5ef"; }
        }

        /// <summary>
        /// Gets uri for SupportObserverResourceAAD app.
        /// We are only hitting this API to access runtime site slot map data.
        /// </summary>
        public string SupportBayApiObserverResourceId
        {
            get { return "https://microsoft.onmicrosoft.com/SupportObserverResourceApp"; }
        }

        public void PostInitialize()
        {
            //no op
        }
    }
}
