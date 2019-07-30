using System;
using System.Threading.Tasks;
using Diagnostics.DataProviders.TokenService;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Diagnostics.DataProviders
{
    public sealed class KustoTokenService : ITokenService
    {
        private static readonly Lazy<KustoTokenService> _instance = new Lazy<KustoTokenService>(() => new KustoTokenService());

        public static KustoTokenService Instance => _instance.Value;

        public TokenRefresher TokenRefresher { get; private set; }

        protected AuthenticationContext AuthenticationContext { get; set; }
        protected ClientCredential ClientCredential { get; set; }
        protected string Resource { get; set; }
        protected string TokenServiceName { get; set; }

        private KustoTokenService() : base()
        {
        }

        public void Initialize(KustoDataProviderConfiguration configuration)
        {
            TokenRefresher = new TokenRefresher(
                configuration.AADAuthority,
                configuration.ClientId,
                configuration.AppKey,
                configuration.AADKustoResource,
                "KustoTokenRefresh");
        }

        public async Task<string> GetAuthorizationTokenAsync()
        {
            return await TokenRefresher.GetAuthorizationTokenAsync().ConfigureAwait(false);
        }
    }
}
