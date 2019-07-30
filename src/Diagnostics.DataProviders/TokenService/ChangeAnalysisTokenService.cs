using System;
using System.Threading.Tasks;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Diagnostics.DataProviders.TokenService
{
    public class ChangeAnalysisTokenService : ITokenService
    {
        private static readonly Lazy<ChangeAnalysisTokenService> instance = new Lazy<ChangeAnalysisTokenService>(() => new ChangeAnalysisTokenService());

        public static ChangeAnalysisTokenService Instance => instance.Value;
        public TokenRefresher TokenRefresher { get; private set; }
        protected AuthenticationContext AuthenticationContext { get; set; }
        protected ClientCredential ClientCredential { get; set; }
        protected string Resource { get; set; }
        protected string TokenServiceName { get; set; }

        public void Initialize(ChangeAnalysisDataProviderConfiguration configuration)
        {
            TokenRefresher = new TokenRefresher(configuration.AADAuthority, configuration.ClientId, configuration.AppKey, configuration.AADChangeAnalysisResource, "ChangeAnalysisTokenRefresh");
        }

        public async Task<string> GetAuthorizationTokenAsync()
        {
            return await TokenRefresher.GetAuthorizationTokenAsync().ConfigureAwait(false);
        }
    }
}
