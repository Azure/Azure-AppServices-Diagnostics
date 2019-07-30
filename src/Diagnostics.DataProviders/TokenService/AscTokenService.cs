using System;
using System.Threading.Tasks;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Diagnostics.DataProviders.TokenService
{
    public class AscTokenService : ITokenService
    {
        private static readonly Lazy<AscTokenService> instance = new Lazy<AscTokenService>(() => new AscTokenService());

        public TokenRefresher TokenRefresher { get; private set; }
        public static AscTokenService Instance => instance.Value;
        protected AuthenticationContext AuthenticationContext { get; set; }
        protected ClientCredential ClientCredential { get; set; }
        protected string Resource { get; set; }
        protected string TokenServiceName { get; set; }

        public void Initialize(AscDataProviderConfiguration configuration)
        {
            TokenRefresher = new TokenRefresher(configuration.AADAuthority, configuration.ClientId, configuration.AppKey, configuration.TokenResource, "AscTokenRefresh");
        }

        public async Task<string> GetAuthorizationTokenAsync()
        {
            return await TokenRefresher.GetAuthorizationTokenAsync().ConfigureAwait(false);
        }
    }
}
