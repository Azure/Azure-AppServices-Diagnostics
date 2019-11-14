using System;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Diagnostics.DataProviders.TokenService
{
    public class SearchServiceTokenService : TokenServiceBase
    {
        private static readonly Lazy<SearchServiceTokenService> instance = new Lazy<SearchServiceTokenService>(() => new SearchServiceTokenService());

        public static SearchServiceTokenService Instance => instance.Value;
        protected override AuthenticationContext AuthenticationContext { get; set; }
        protected override ClientCredential ClientCredential { get; set; }
        protected override string Resource { get; set; }
        protected override string TokenServiceName { get; set; }

        public void Initialize(SearchServiceProviderConfiguration searchServiceProviderConfiguration)
        {
            Resource = searchServiceProviderConfiguration.AADResource;
            AuthenticationContext = new AuthenticationContext(searchServiceProviderConfiguration.AADAuthority);
            ClientCredential = new ClientCredential(searchServiceProviderConfiguration.ClientId,
                                                    searchServiceProviderConfiguration.AppKey);
            TokenServiceName = "SearchServiceTokenRefresh";
            StartTokenRefresh();
        }
    }
}
