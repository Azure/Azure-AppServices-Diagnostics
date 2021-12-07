using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest.Azure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class KeyVaultCertificateConfigProviderExtensions
    {
        /// <summary>
        /// Extends the IConfigurationBuilder object to auto fetch key vault certificates.
        /// Retieves PFX certificates from KeyVault and installs them into current-user certificate store. Cert store cleanup is performed everytime the application is started.
        /// This should only be called in dev environment.
        /// </summary>
        /// <param name="builder">IConfigurationBuilder object</param>
        /// <param name="keyVaultUri">KeyVault URI from where certificates are to be retrieved.</param>
        /// <param name="keyVaultClient">KeyVault client used to communicate with the desired Azure KeyVault.</param>
        /// <returns></returns>
        public static IConfigurationBuilder AddAndInstallAzureKeyVaultCertificates(this IConfigurationBuilder builder, string keyVaultUri, KeyVaultClient keyVaultClient)
        {
            return builder.Add(new AzureKeyVaultCertificatesConfigProvider(keyVaultUri, keyVaultClient));
        }
    }

    /// <summary>
    /// Configuration provider that helps auto-setup dev environment by installing PFX certificates from KeyVault into the current-user cert store.
    /// Cert store cleanup is performed everytime the application is started.
    /// </summary>
    public class AzureKeyVaultCertificatesConfigProvider : ConfigurationProvider, IConfigurationSource
    {
        private string _keyVaultUri = string.Empty;
        private KeyVaultClient _keyVaultClient = null;

        private const string Cert_FriendlyName_Prefix = "DevKV-";

        public AzureKeyVaultCertificatesConfigProvider()
        { 
        }

        /// <summary>
        /// Open a the current-user cert store in read write mode if it is not already open.
        /// </summary>
        /// <param name="certStore"></param>
        /// <returns></returns>
        private X509Store GetOpenedUserCertStore(X509Store certStore = null)
        {
            certStore = certStore ?? new X509Store(StoreName.My, StoreLocation.CurrentUser);

            if (!certStore.IsOpen)
            {
                certStore.Open(OpenFlags.ReadWrite);
            }

            return certStore;
        }

        /// <summary>
        /// Extract the subject name from supplied PFX certificate in a format compatible to use in certStore.Certificates.Find with X509FindType.FindBySubjectName option.
        /// </summary>
        /// <param name="kvPfxCert">PFX certificate whose subject name is to be looked up in local cert store.</param>
        /// <returns>Subject name from supplied PFX certificate in a format compatible to use in certStore.Certificates.Find with X509FindType.FindBySubjectName option.</returns>
        private string GetSubjectNameForSearch(X509Certificate2 kvPfxCert)
        {
            if (!string.IsNullOrWhiteSpace(kvPfxCert?.Subject) && kvPfxCert.Subject.StartsWith("CN=", StringComparison.CurrentCultureIgnoreCase))
            {
                return kvPfxCert.Subject.Substring(3);
            }
            return string.Empty;            
        }

        /// <summary>
        /// Clean up earlier versions of the supplied certificate from the certificate store.
        /// If the supplied certificate version and one found in cert store are the same and are valid, no action is taken.
        /// </summary>
        /// <param name="certStore">Cert store object where the matching certificate is to be cleaned up if required.</param>
        /// <param name="kvPfxCert">Certificate that should be cleaned up if required.</param>
        private void CleanupMatchingInvalidCertFromLocalMachine(X509Store certStore, X509Certificate2 kvPfxCert)
        {
            if (kvPfxCert != null && kvPfxCert.HasPrivateKey)
            {
                bool wasCertStoreOpen = certStore?.IsOpen == true;
                certStore = GetOpenedUserCertStore(certStore);

                //Get only valid certs matching either the subject name or the thumbprint.
                X509Certificate2Collection certCollectionValidOnly = string.IsNullOrWhiteSpace(kvPfxCert.SubjectName.Name)
                    ? certStore.Certificates.Find(X509FindType.FindByThumbprint, kvPfxCert.Thumbprint, true)
                    : certStore.Certificates.Find(X509FindType.FindBySubjectName, GetSubjectNameForSearch(kvPfxCert), true);

                //Get all certs matching either the subject name or the thumbprint.
                X509Certificate2Collection certCollectionAll = string.IsNullOrWhiteSpace(kvPfxCert.SubjectName.Name)
                    ? certStore.Certificates.Find(X509FindType.FindByThumbprint, kvPfxCert.Thumbprint, false)
                    : certStore.Certificates.Find(X509FindType.FindBySubjectName, GetSubjectNameForSearch(kvPfxCert), false);

                //Remove all invalid certs
                foreach (X509Certificate2 currCert in certCollectionAll)
                {
                    if (!certCollectionValidOnly.Contains(currCert))
                    {
                        //Current certificate is not valid, clean it up.
                        certStore.Remove(currCert);
                    }
                }

                //Remove valid certs if the KV cert is more recent or has a different thumbprint than the one installed.
                foreach (X509Certificate2 currCert in certCollectionValidOnly)
                {
                    if (DateTime.Parse(kvPfxCert.GetExpirationDateString()) > DateTime.Parse(currCert.GetExpirationDateString())
                        || !kvPfxCert.Thumbprint.Equals(currCert.Thumbprint, StringComparison.OrdinalIgnoreCase)
                        )
                    {
                        certStore.Remove(currCert);
                    }
                }

                if (!wasCertStoreOpen && certStore.IsOpen)
                {
                    //Cert store was passed to this method in unopened state. Retain the state
                    certStore.Close();
                }
            }
        }

        /// <summary>
        /// Fetch certificate from Azure KV and install it in current-user store.
        /// Performs the necessary cleanup to ensure dev boxes are do not have stale certificates while retaining only the most up to date PFX certificates.
        /// </summary>
        public override async void Load()
        {
            ConcurrentDictionary<string, X509Certificate2> allKeyVaultCerts = new ConcurrentDictionary<string, X509Certificate2>();
            List<Task<SecretBundle>> keyVaultSecretTasks = new List<Task<SecretBundle>>();
            if (!string.IsNullOrWhiteSpace(_keyVaultUri) && _keyVaultClient != null)
            {                
                IPage<CertificateItem> certsList = await _keyVaultClient.GetCertificatesAsync(_keyVaultUri).ConfigureAwait(true);
                foreach (CertificateItem certItem in certsList)
                {
                    if (certItem.Attributes.Enabled == true)
                    {
                        keyVaultSecretTasks.Add(_keyVaultClient.GetSecretAsync(_keyVaultUri, certItem.Identifier.Name));
                    }
                }
                if (keyVaultSecretTasks.Count > 0)
                {
                    X509Store certStore = GetOpenedUserCertStore();
                    try
                    {
                        IEnumerable<SecretBundle> secretList = await Task.WhenAll(keyVaultSecretTasks.ToArray()).ConfigureAwait(true);

                        foreach (SecretBundle secret in secretList)
                        {
                            byte[] pfxCertBytes = Convert.FromBase64String(secret.Value);
                            using (X509Certificate2 kvPfxCert = new X509Certificate2(pfxCertBytes, password: (string)null, keyStorageFlags: X509KeyStorageFlags.PersistKeySet))
                            {
                                kvPfxCert.FriendlyName = $"{Cert_FriendlyName_Prefix}{secret.SecretIdentifier.Name}";
                                CleanupMatchingInvalidCertFromLocalMachine(certStore, kvPfxCert);

                                //Install the certificate if the cleanup routine ended up removing all and there is no cert that matches the current KV cert in current-user store.
                                //If a cert exists, then no need to install it again.
                                X509Certificate2Collection certCollectionValidOnly = string.IsNullOrWhiteSpace(kvPfxCert.SubjectName.Name)
                                    ? certStore.Certificates.Find(X509FindType.FindBySerialNumber, kvPfxCert.Thumbprint, true)
                                    : certStore.Certificates.Find(X509FindType.FindBySubjectName, GetSubjectNameForSearch(kvPfxCert), true);
                                if (certCollectionValidOnly.Count == 0)
                                {
                                    certStore.Add(kvPfxCert);
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (certStore.IsOpen)
                        {
                            certStore.Close();
                        }
                        certStore.Dispose();
                    }
                }
            }
        }

        public AzureKeyVaultCertificatesConfigProvider(string keyVaultUri, KeyVaultClient keyVaultClient)
        {
            _keyVaultClient = keyVaultClient;
            _keyVaultUri = keyVaultUri;            
        }

        IConfigurationProvider IConfigurationSource.Build(IConfigurationBuilder builder)
        {
            return new AzureKeyVaultCertificatesConfigProvider(_keyVaultUri, _keyVaultClient);
        }
    }    
}
