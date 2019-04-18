using System;
using Diagnostics.DataProviders.TokenService;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
namespace Diagnostics.DataProviders
{
    public class KustoTokenService : TokenServiceBase
    {
        private static readonly Lazy<KustoTokenService> _instance = new Lazy<KustoTokenService>(() => new KustoTokenService());

        public static KustoTokenService Instance => _instance.Value;

        protected override AuthenticationContext AuthenticationContext { get; set; }
        protected override ClientCredential ClientCredential { get; set; }
        protected override string Resource { get; set; }
        protected override string TokenServiceName { get; set; }

        private KustoTokenService() : base()
        {
        }

        public void Initialize(KustoDataProviderConfiguration configuration)
        {
            AuthenticationContext = new AuthenticationContext(configuration.AADAuthority);
            ClientCredential = new ClientCredential(configuration.ClientId, configuration.AppKey);
            Resource = configuration.AADKustoResource;
            TokenServiceName = "KustoTokenRefresh";
            StartTokenRefresh();
        }
    }
}