using System;
using System.Security.Cryptography.X509Certificates;
using Diagnostics.Logger;
namespace Diagnostics.DataProviders.KeyVaultCertLoader
{
    public abstract class CertLoaderBase
    {
        protected abstract string Thumbprint { get; set; }
        protected abstract string SubjectName { get; set; }

        public X509Certificate2 Cert { get; private set; }

        public void LoadCertFromAppService()
        {
            X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadOnly);

            try
            {
                if (!string.IsNullOrWhiteSpace(SubjectName) && SubjectName.StartsWith("CN=", StringComparison.CurrentCultureIgnoreCase))
                {
                    SubjectName = SubjectName.Substring(3);
                }

                X509Certificate2Collection certCollection = string.IsNullOrWhiteSpace(SubjectName) ? certStore.Certificates.Find(
                                                            X509FindType.FindByThumbprint,
                                                            Thumbprint,
                                                            true) : certStore.Certificates.Find(
                                                            X509FindType.FindBySubjectName,
                                                            SubjectName,
                                                            true);

                // Get the first cert with the thumbprint
                if (certCollection.Count > 0)
                {
                    Cert = certCollection[0];
                    DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Successfully loaded Cert with thumbprint {Thumbprint}");
                }
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Error: {ex.Message} occurred while trying to load cert {Thumbprint}, Stack Trace: {ex.StackTrace} ");
                throw;
            }
            finally
            {
                certStore.Close();
            }
            
        }
    }
}
