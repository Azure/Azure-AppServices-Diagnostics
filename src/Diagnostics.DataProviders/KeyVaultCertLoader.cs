using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;

namespace Diagnostics.DataProviders
{
    //Loads the cert from key vault.
    public class KeyVaultCertLoader
    {
        private static readonly Lazy<KeyVaultCertLoader> _instance = new Lazy<KeyVaultCertLoader>(() => new KeyVaultCertLoader());

        public static KeyVaultCertLoader Instance => _instance.Value;

        public X509Certificate2 GeoCert { get; private set; }

        public X509Certificate2 MdmCert { get; private set; }
        
        public async void LoadCertFromKeyVaultAsync(IConfiguration configuration)
        {
            try
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var prodKeyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                var geoCertificateSecret = await prodKeyVaultClient.GetSecretAsync($"https://{configuration["Secrets:ProdKeyVaultName"]}.vault.azure.net", configuration["GeoMaster:CertificateName"]);
                var mdmCertificateSecret = await prodKeyVaultClient.GetSecretAsync($"https://{configuration["Secrets:ProdKeyVaultName"]}.vault.azure.net", configuration["Mdm:CertificateName"]);
                GeoCert = new X509Certificate2(Convert.FromBase64String(geoCertificateSecret.Value));
                MdmCert = new X509Certificate2(Convert.FromBase64String(mdmCertificateSecret.Value));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
