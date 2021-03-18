using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.Logger;
using Microsoft.Azure.OperationalInsights;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders.TokenService
{
    class K8SETokenService : LogAnalyticsTokenServiceBase
    {
        private static readonly Lazy<K8SETokenService> instance = new Lazy<K8SETokenService>(() => new K8SETokenService());
        public static K8SETokenService Instance => instance.Value;

        protected override string workspaceId { get; set; }
        protected override string clientId { get; set; }
        protected override string clientSecret { get; set; }

        protected override string domain { get; set; }
        protected override string authEndpoint { get; set; }
        protected override string tokenAudience { get; set; }

        protected override ActiveDirectoryServiceSettings adSettings { get; set; }
        protected override Microsoft.Rest.ServiceClientCredentials creds { get; set; }

        protected override OperationalInsightsDataClient client { get; set; }


        protected override string TokenServiceName { get; set; }

        public OperationalInsightsDataClient GetClient()
        {
            return client;
        }

        public void Initialize(K8SELogAnalyticsDataProviderConfiguration k8SELogAnalyticsDataProviderConfiguration)
        {
            workspaceId = k8SELogAnalyticsDataProviderConfiguration.WorkspaceId;
            clientId = k8SELogAnalyticsDataProviderConfiguration.ClientId;
            clientSecret = k8SELogAnalyticsDataProviderConfiguration.ClientSecret;

            domain = k8SELogAnalyticsDataProviderConfiguration.Domain;
            authEndpoint = k8SELogAnalyticsDataProviderConfiguration.AuthEndpoint;
            tokenAudience = k8SELogAnalyticsDataProviderConfiguration.TokenAudience;

            TokenServiceName = "K8SELogAnalyticsTokenRefresh";

            StartTokenRefresh();
        }
        
    }
}
