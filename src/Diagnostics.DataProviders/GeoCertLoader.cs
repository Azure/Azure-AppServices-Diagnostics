using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;

namespace Diagnostics.DataProviders
{
    public class GeoCertLoader
    {
        private static readonly Lazy<GeoCertLoader> _instance = new Lazy<GeoCertLoader>(() => new GeoCertLoader());

        public static GeoCertLoader Instance => _instance.Value;

        public X509Certificate2 GeoCert { get; private set; }
        
        public void LoadCertFromKeyVault(IConfiguration configuration)
        {
            try
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var prodKeyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                var certificateSecret = prodKeyVaultClient.GetSecretAsync($"https://{configuration["Secrets:ProdKeyVaultName"]}.vault.azure.net", configuration["GeoMaster:CertificateName"]).Result;
                var privateKeyBytes = Convert.FromBase64String(certificateSecret.Value);
                GeoCert = new X509Certificate2(privateKeyBytes);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
