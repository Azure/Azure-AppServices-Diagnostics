using System;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Diagnostics.DataProviders.DataProviderConfigurations;
namespace Diagnostics.DataProviders.TokenService
{
    public class ChangeAnalysisTokenService: TokenServiceBase
    {
        private static readonly Lazy<ChangeAnalysisTokenService> instance = new Lazy<ChangeAnalysisTokenService>(() => new ChangeAnalysisTokenService());

        public static ChangeAnalysisTokenService Instance => instance.Value;
        protected override AuthenticationContext AuthenticationContext { get; set; }
        protected override ClientCredential ClientCredential { get; set; }
        protected override string Resource { get; set; }
        protected override string TokenServiceName { get; set; }

        public void Initialize(ChangeAnalysisDataProviderConfiguration changeAnalysisDataProviderConfiguration)
        {
            Resource = changeAnalysisDataProviderConfiguration.AADChangeAnalysisResource;
            AuthenticationContext = new AuthenticationContext(changeAnalysisDataProviderConfiguration.AADAuthority);
            ClientCredential = new ClientCredential(changeAnalysisDataProviderConfiguration.ClientId,
                                                    changeAnalysisDataProviderConfiguration.AppKey);
            TokenServiceName = "ChangeAnalysisTokenRefresh";
            StartTokenRefresh();
        }
    }
}
