using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Kusto.Data;
using Microsoft.Azure.OperationalInsights;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Data;
using System.Threading.Tasks;
using Diagnostics.DataProviders.TokenService;
using System.Diagnostics;
using Microsoft.Azure.OperationalInsights.Models;
using Diagnostics.Logger;

namespace Diagnostics.DataProviders
{
    public class K8SELogAnalyticsClient : LogAnalyticsClientBase
    {
        private Microsoft.Rest.ServiceClientCredentials creds;
        private string workspaceId;
        public override OperationalInsightsDataClient client { get; set; }
        public override string _requestId { get; set; }

        public K8SELogAnalyticsClient(string requestId)
        {
            creds = K8SELogAnalyticsTokenService.Instance.getCreds();
            workspaceId = K8SELogAnalyticsTokenService.Instance.getWorkspaceId();
            client = new OperationalInsightsDataClient(creds);
            client.WorkspaceId = workspaceId;
            _requestId = requestId;
        }
    }
}

