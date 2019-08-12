using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace Diagnostics.DataProviders.KeyVaultCertLoader
{
    public abstract class KeyVaultCertLoaderBase
    {
        protected abstract string KeyVault { get; set; }

        protected abstract string CertificateName { get; set; }

        public X509Certificate2 Cert { get; private set; }

        public async void LoadCertFromKeyVault()
        {
            try
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var prodKeyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                var certificateSecret = await prodKeyVaultClient.GetSecretAsync($"https://{KeyVault}.vault.azure.net", CertificateName);
                var privateKeyBytes = Convert.FromBase64String(certificateSecret.Value);
                Cert = new X509Certificate2(privateKeyBytes, string.Empty, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
