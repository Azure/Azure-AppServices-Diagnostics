// <copyright file="SupportObserverDataProviderConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

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
        /// Is the local host version of observer enabled.
        /// </summary>
        [ConfigurationName("ObserverLocalHostEnabled", DefaultValue = false)]
        public bool ObserverLocalHostEnabled { get; set; }

        /// <summary>
        /// Gets or sets tenant to authenticate with.
        /// </summary>
        public string AADAuthority { get; set; }


        [ConfigurationName("AADResource")]
        public string AADResource { get; set; }

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
