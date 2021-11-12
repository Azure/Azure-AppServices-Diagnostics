using System;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Extensions.Configuration;
using Diagnostics.DataProviders.TokenService;

namespace Diagnostics.RuntimeHost.Services
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
            if (!configuration.GetValue("CompilerHost:UseCertAuth", false))
            {
                Resource = configuration["CompilerHost:AADResource"];
                AuthenticationContext = new AuthenticationContext(configuration["CompilerHost:AADAuthority"]);
                ClientCredential = new ClientCredential(configuration["CompilerHost:ClientId"], configuration["CompilerHost:AppKey"]);
                TokenServiceName = "CompilerHostTokenService";
                StartTokenRefresh();
            }
        }
    }
}
