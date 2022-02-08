using System;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.DataProviders.TokenService;

namespace Diagnostics.RuntimeHost.Services
{
    public class GraphAPITokenService : TokenServiceBase
    {
        private static readonly Lazy<GraphAPITokenService> instance = new Lazy<GraphAPITokenService>(() => new GraphAPITokenService());

        public static GraphAPITokenService Instance => instance.Value;

        /// <inheritdoc/>
        protected override AuthenticationContext AuthenticationContext { get; set; }

        /// <inheritdoc/>
        protected override ClientCredential ClientCredential { get; set; }

        /// <inheritdoc/>
        protected override string Resource { get; set; }

        protected override string TokenServiceName { get; set; }

        /// <summary>
        /// Initializes Graph Token Service with provided config.
        /// </summary>
        public void Initialize(IConfiguration configuration)
        {
            AuthenticationContext = new AuthenticationContext(GraphConstants.MicrosoftTenantAuthorityUrl);
            ClientCredential = new ClientCredential(configuration["Durian:GraphAPIClientId"], configuration["Durian:GraphAPIClientSecret"]);
            Resource = GraphConstants.DefaultGraphEndpoint;
            StartTokenRefresh();
        }
    }
}
