using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Diagnostics.DataProviders;
using System.Diagnostics;
using System;

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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var r = WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var builtConfig = config
                        .AddCommandLine(args)
                        .AddEnvironmentVariables()
                        .Build();

                    
                        var azureServiceTokenProvider = new AzureServiceTokenProvider();
                        var keyVaultClient = new KeyVaultClient(
                            new KeyVaultClient.AuthenticationCallback(
                                azureServiceTokenProvider.KeyVaultTokenCallback));

                    string keyVaultConfig = context.HostingEnvironment.IsProduction() ? "Secrets:ProdKeyVaultName" : "Secrets:DevKeyVaultName";

                    

                    config.AddAzureKeyVault(
                            $"https://{builtConfig[keyVaultConfig]}.vault.azure.net/",
                            keyVaultClient,
                            new DefaultKeyVaultSecretManager());
                    
                })
                .UseStartup<Startup>()
                .Build();

            sw.Stop();
            Console.WriteLine("azure key vault 1 loaded: " + sw.ElapsedMilliseconds + "ms");

            return r;
        }
    }
}
