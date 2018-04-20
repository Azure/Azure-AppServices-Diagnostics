using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    [DataSourceConfiguration("SupportObserver")]
    public class SupportObserverDataProviderConfiguration : IDataProviderConfiguration
    {
        private static AuthenticationContext _authContext;
        private static ClientCredential _aadCredentials;
        private object _lockObject = new object();
        public SupportObserverDataProviderConfiguration()
        {
        }

        /// <summary>
        /// Client Id
        /// </summary>
        [ConfigurationName("ClientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// App Key
        /// </summary>
        [ConfigurationName("AppKey")]
        public string AppKey { get; set; }

        [ConfigurationName("IsProdConfigured", DefaultValue = true)]
        public bool IsProdConfigured { get; set; }

        [ConfigurationName("IsTestConfigured", DefaultValue = false)]
        public bool IsTestConfigured { get; set; }

        [ConfigurationName("IsMockConfigured", DefaultValue = false)]
        public bool IsMockConfigured { get; set; }

        /// <summary>
        /// ResourceId for WAWSObserver AAD app
        /// </summary>
        public string ResourceId { get { return "d1abfd91-e19c-426e-802f-a6c55421a5ef"; } }
        /// <summary>
        /// Uri for SupportObserverResourceAAD app. 
        /// We are only hitting this API to access runtime site slot map data
        /// </summary>
        public string RuntimeSiteSlotMapResourceUri { get { return "https://microsoft.onmicrosoft.com/SupportObserverResourceApp"; } }

        /// <summary>
        /// Bearer token for observer API call
        /// </summary>
        internal async Task<string> GetAccessToken(string resourceId = null)
        {
            if (IsProdConfigured)
            {
                if (_authContext == null)
                {
                    lock (_lockObject)
                    {
                        if (_authContext == null)
                        {
                            _aadCredentials = new ClientCredential(ClientId, AppKey);
                            _authContext = new AuthenticationContext("https://login.microsoftonline.com/microsoft.onmicrosoft.com", TokenCache.DefaultShared);
                        }
                    }
                }
            }
            else
            {
                return await Task.FromResult("abcdtoken");
            }

            var authResult = await _authContext.AcquireTokenAsync(resourceId ?? ResourceId, _aadCredentials);
            return authResult.AccessToken;
        }

        public void PostInitialize()
        {
            //no op
        }
    }
}
