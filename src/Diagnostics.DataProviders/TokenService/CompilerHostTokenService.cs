using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using Diagnostics.DataProviders.DataProviderConfigurations;

namespace Diagnostics.DataProviders.TokenService
{
    // Class that acquires token to authenticate with CompilerHost
    public class CompilerHostTokenService : TokenServiceBase
    {
        private static readonly Lazy<CompilerHostTokenService> instance = new Lazy<CompilerHostTokenService>(() => new CompilerHostTokenService());

        public static CompilerHostTokenService Instance => instance.Value;
        protected override AuthenticationContext AuthenticationContext { get; set ; }
        protected override ClientCredential ClientCredential { get; set ; }
        protected override string Resource { get; set; }
        protected override string TokenServiceName { get; set; }

        public void Initialize(CompilerHostAuthConfiguration compilerHostAuthConfiguration)
        {
            Resource = compilerHostAuthConfiguration.AADResource;
            AuthenticationContext = new AuthenticationContext(compilerHostAuthConfiguration.AADAuthority);
            ClientCredential = new ClientCredential(compilerHostAuthConfiguration.ClientId, compilerHostAuthConfiguration.AppKey);
            TokenServiceName = "CompilerHostTokenService";
            StartTokenRefresh();
        }
    }
}
