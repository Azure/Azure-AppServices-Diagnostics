using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Logging;
using Diagnostics.DataProviders;
using System;
using Microsoft.CodeAnalysis;
using Diagnostics.DataProviders.Utility;
using Diagnostics.Logger;

namespace Diagnostics.RuntimeHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                CreateWebHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                //Log unhandled exceptions on startup
                DiagnosticsETWProvider.Instance.LogRuntimeHostUnhandledException(
                    string.Empty,
                    "LogException_Startup",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    ex.GetType().ToString(),
                    ex.ToString());
            
                throw;
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var builtConfig = config.Build();
                    // For production and staging, skip outbound call to keyvault for AppSettings
                    if(builtConfig.GetValue<bool>("Secrets:KeyVaultEnabled", true) || context.HostingEnvironment.IsDevelopment())
                    {
                        var (keyVaultUri, keyVaultClient) = GetKeyVaultSettings(context, builtConfig);
                        config
                            .AddAzureKeyVault(
                                keyVaultUri,
                                keyVaultClient,
                                new DefaultKeyVaultSecretManager());
                    }
                        config.AddEnvironmentVariables()
                        .AddCommandLine(args)
                        .Build();
                })
                .UseStartup<Startup>();
        }

        private static Tuple<string, KeyVaultClient> GetKeyVaultSettings(WebHostBuilderContext context, IConfigurationRoot builtConfig)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider(azureAdInstance: builtConfig["Secrets:AzureAdInstance"]);
            var keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(
                    azureServiceTokenProvider.KeyVaultTokenCallback));

            string keyVaultConfig = Helpers.GetKeyvaultforEnvironment(context.HostingEnvironment.EnvironmentName);
            return new Tuple<string, KeyVaultClient>(builtConfig[keyVaultConfig], keyVaultClient);
        }
    }
}
