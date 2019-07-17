using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Microsoft.Extensions.Configuration;
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

        public void Initialize(IConfiguration configuration)
        {
            Resource = configuration["CompilerHostAuth:AADResource"];
            AuthenticationContext = new AuthenticationContext(configuration["CompilerHostAuth:AADAuthority"]);
            ClientCredential = new ClientCredential(configuration["CompilerHostAuth:ClientId"], configuration["CompilerHostAuth:AppKey"]);
            TokenServiceName = "CompilerHostTokenService";
            StartTokenRefresh();
        }
    }
}
