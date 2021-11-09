using Diagnostics.DataProviders.KeyVaultCertLoader;
using Diagnostics.Logger;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Diagnostics.DataProviders
{
    public class GenericCertLoader: CertLoaderBase
    {
        private static readonly Lazy<GenericCertLoader> _instance = new Lazy<GenericCertLoader>(() => new GenericCertLoader());

        public static GenericCertLoader Instance => _instance.Value;

        protected override string Thumbprint { get; set; }
        protected override string SubjectName { get; set; }

        public new void LoadCertFromAppService()
        {
            X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadOnly);
            try
            {
                foreach (X509Certificate2 currCert in certStore.Certificates)
                {
                    //Do not reload a cert if one with the subject name already exists since this method is supoposed to be called only once.
                    if (!_certCollection.ContainsKey(currCert.Subject.ToLower()))
                    {
                        if (_certCollection.TryAdd(currCert.Subject.ToLower(), currCert))
                        {
                            DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Successfully loaded cert with thumbprint {currCert.Thumbprint} Subjectname: {currCert.Subject}");
                        }
                        else
                        {
                            DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Failed to add cert with thumbprint {currCert.Thumbprint} Subjectname: {currCert.Subject} to the cert collection.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Error: {ex.Message} occurred while trying to load certs. Stack Trace: {ex.StackTrace} ");
                throw;
            }
            finally
            {
                certStore.Close();
                certStore.Dispose();
            }
        }

        private ConcurrentDictionary<string, X509Certificate2> _certCollection = new ConcurrentDictionary<string, X509Certificate2>();

        public void Initialize()
        {
            LoadCertFromAppService();
        }

#pragma warning disable CA1303 // Do not pass literals as localized parameters
        public X509Certificate2 GetCertBySubjectName(string subjectName)
        {
            if (!string.IsNullOrWhiteSpace(subjectName))
            {
                if (subjectName.StartsWith("CN=", StringComparison.CurrentCultureIgnoreCase))
                {
                    subjectName = subjectName.Substring(3);
                }
                subjectName = subjectName.ToLower();
                return _certCollection.TryGetValue(subjectName, out X509Certificate2 requestedCert) ? requestedCert : throw new KeyNotFoundException($"Certificate matching {subjectName} subject name was not found. Please validate the subject name.");
            }
            else
            {
                throw new ArgumentNullException(paramName: nameof(subjectName), message: "Subject name is null or empty. Please supply a valid subject name to lookup");
            }
        }
#pragma warning restore CA1303


#pragma warning disable CA1303 // Do not pass literals as localized parameters
        public X509Certificate2 GetCertByThumbprint(string thumbprint)
            => string.IsNullOrWhiteSpace(thumbprint)
            ? throw new ArgumentNullException(paramName: nameof(thumbprint), message: "Thumbprint is null or empty. Please supply a valid thumbprint to lookup")
            : _certCollection.Values.Where(cert => cert.Thumbprint.Equals(thumbprint, StringComparison.OrdinalIgnoreCase))?.First() ?? throw new KeyNotFoundException(message: $"Certificate matching the {thumbprint} thumbprint was not found. Please validate the thumbprint.");
#pragma warning restore CA1303

    }
}
