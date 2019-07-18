using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Diagnostics.DataProviders;

namespace Diagnostics.RuntimeHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var builtConfig = config
                        .AddCommandLine(args)
                        .Build();

                    
                        var azureServiceTokenProvider = new AzureServiceTokenProvider();
                        var keyVaultClient = new KeyVaultClient(
                            new KeyVaultClient.AuthenticationCallback(
                                azureServiceTokenProvider.KeyVaultTokenCallback));

                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        config.AddAzureKeyVault(
                            $"https://{builtConfig["Secrets:DevKeyVaultName"]}.vault.azure.net/",
                            keyVaultClient,
                            new DefaultKeyVaultSecretManager());
                    }
                    else if (context.HostingEnvironment.IsProduction())
                    {
                        config.AddAzureKeyVault(
                            $"https://{builtConfig["Secrets:ProdKeyVaultName"]}.vault.azure.net/",
                            keyVaultClient,
                            new DefaultKeyVaultSecretManager());
                    }
                })
                .UseStartup<Startup>()
                .Build();
        }
    }
}
