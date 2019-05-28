using System;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Diagnostics.DataProviders.DataProviderConfigurations;

namespace Diagnostics.DataProviders.TokenService
{
    public class AscTokenService : TokenServiceBase
    {
        private static readonly Lazy<AscTokenService> instance = new Lazy<AscTokenService>(() => new AscTokenService());

        public static AscTokenService Instance => instance.Value;
        protected override AuthenticationContext AuthenticationContext { get; set; }
        protected override ClientCredential ClientCredential { get; set; }
        protected override string Resource { get; set; }
        protected override string TokenServiceName { get; set; }

        public void Initialize(AscDataProviderConfiguration ascDataProviderConfiguration)
        {
            Resource = ascDataProviderConfiguration.TokenResource;
            AuthenticationContext = new AuthenticationContext(ascDataProviderConfiguration.AADAuthority);
            ClientCredential = new ClientCredential(ascDataProviderConfiguration.ClientId, ascDataProviderConfiguration.AppKey);
            TokenServiceName = "AscTokenRefresh";
            StartTokenRefresh();
        }        
    }
}
