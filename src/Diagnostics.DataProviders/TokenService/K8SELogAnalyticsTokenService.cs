using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Text;
using Diagnostics.DataProviders.DataProviderConfigurations;

namespace Diagnostics.DataProviders.TokenService
{
    public class K8SELogAnalyticsTokenService : TokenServiceBase
    {
        private static readonly Lazy<K8SELogAnalyticsTokenService> instance = new Lazy<K8SELogAnalyticsTokenService>(() => new K8SELogAnalyticsTokenService());

        public static K8SELogAnalyticsTokenService Instance => instance.Value;
        protected override AuthenticationContext AuthenticationContext { get; set; }
        protected override ClientCredential ClientCredential { get; set; }
        protected override string Resource { get; set; }
        protected override string TokenServiceName { get; set; }

        public void Initialize(K8SELogAnalyticsDataProviderConfiguration k8SELogAnalyticsDataProviderConfiguration)
        {
            Resource = k8SELogAnalyticsDataProviderConfiguration.WorkspaceId;
            AuthenticationContext = new AuthenticationContext("https://login.microsoftonline.com/microsoft.onmicrosoft.com");
            //AuthenticationContext = new AuthenticationContext(k8SELogAnalyticsDataProviderConfiguration.Domain);
            ClientCredential = new ClientCredential(k8SELogAnalyticsDataProviderConfiguration.ClientId,
                                                    k8SELogAnalyticsDataProviderConfiguration.ClientSecret);
            TokenServiceName = "K8SELogAnalyticsTokenRefresh";
            StartTokenRefresh();
        }
    }
}
