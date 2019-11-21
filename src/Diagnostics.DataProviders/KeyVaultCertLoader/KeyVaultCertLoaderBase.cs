using System;
using System.Security.Cryptography.X509Certificates;
using Diagnostics.Logger;
namespace Diagnostics.DataProviders.KeyVaultCertLoader
{
    public abstract class KeyVaultCertLoaderBase
    {
        protected abstract string Thumbprint { get; set; }
        public X509Certificate2 Cert { get; private set; }

        public async void LoadCertFromAppService()
        {
            X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadOnly);

            try
            {
                X509Certificate2Collection certCollection = certStore.Certificates.Find(
                                                            X509FindType.FindByThumbprint,
                                                            Thumbprint,
                                                            false);

                // Get the first cert with the thumbprint
                if (certCollection.Count > 0)
                {
                    Cert = certCollection[0];
                    DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Successfully loaded Cert with thumbprint {Thumbprint}");
                }
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Error: {ex.Message} occurred while trying to load cert {Thumbprint} ");
                throw;
            }
            finally
            {
                certStore.Close();
            }
            
        }
    }
}
