// <copyright file="ObserverTokenService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using Diagnostics.DataProviders.TokenService;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Diagnostics.DataProviders
{
    public class ObserverTokenService : TokenServiceBase, IWawsObserverTokenService, ISupportBayApiObserverTokenService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObserverTokenService"/> class.
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="configuration"></param>
        public ObserverTokenService(string resourceId, SupportObserverDataProviderConfiguration configuration)
        {
            Resource = resourceId;
            Initialize(configuration);
        }

        protected override AuthenticationContext AuthenticationContext { get; set; }
        protected override ClientCredential ClientCredential { get; set; }
        protected override string Resource { get; set; }
        protected override string TokenServiceName { get; set; }

        private void Initialize(SupportObserverDataProviderConfiguration configuration)
        {
            AuthenticationContext = new AuthenticationContext(configuration.AADAuthority);
            ClientCredential = new ClientCredential(configuration.ClientId, configuration.AppKey);
            TokenServiceName = "ObserverTokenRefresh";
            StartTokenRefresh();
        }
    }
}
