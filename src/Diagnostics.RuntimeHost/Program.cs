using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Diagnostics.DataProviders;
using System;
using Microsoft.CodeAnalysis;
using Diagnostics.DataProviders.Utility;

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
                    var (keyVaultConfig, keyVaultClient) = GetKeyVaultSettings(context, config);

                    config
                        .AddAzureKeyVault(
                            $"https://{keyVaultConfig}.vault.azure.net/",
                            keyVaultClient,
                            new DefaultKeyVaultSecretManager())
                        .AddEnvironmentVariables()
                        .AddCommandLine(args)
                        .Build();
                })
                .UseStartup<Startup>()
                .Build();
        }

        private static Tuple<string, KeyVaultClient> GetKeyVaultSettings(WebHostBuilderContext context, IConfigurationBuilder config)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(
                    azureServiceTokenProvider.KeyVaultTokenCallback));

            string keyVaultConfig = Helpers.GetKeyvaultforEnvironment(context.HostingEnvironment.EnvironmentName);

            var builtConfig = config.Build();

            return new Tuple<string, KeyVaultClient>(builtConfig[keyVaultConfig], keyVaultClient);
        }
    }
}
