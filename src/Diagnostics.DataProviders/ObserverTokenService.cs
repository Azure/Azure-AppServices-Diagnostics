// <copyright file="ObserverTokenService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System.Threading.Tasks;
using Diagnostics.DataProviders.TokenService;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Diagnostics.DataProviders
{
    public class ObserverTokenService : ITokenService, IWawsObserverTokenService, ISupportBayApiObserverTokenService
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

        public TokenRefresher TokenRefresher { get; private set; }
        protected AuthenticationContext AuthenticationContext { get; set; }
        protected ClientCredential ClientCredential { get; set; }
        protected string Resource { get; set; }
        protected string TokenServiceName { get; set; }

        public async Task<string> GetAuthorizationTokenAsync()
        {
            return await TokenRefresher.GetAuthorizationTokenAsync().ConfigureAwait(false);
        }

        private void Initialize(SupportObserverDataProviderConfiguration configuration)
        {
            TokenRefresher = new TokenRefresher(
                configuration.AADAuthority,
                configuration.ClientId,
                configuration.AppKey,
                Resource,
                "ObserverTokenRefresh");
        }
    }
}
