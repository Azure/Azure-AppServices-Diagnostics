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
        private OperationalInsightsDataClient _client;
        public override OperationalInsightsDataClient client
        {
            get
            {
                if (_client == null)
                {
                    _client = new OperationalInsightsDataClient(creds);
                    _client.WorkspaceId = workspaceId;
                }

                return _client;
            }

            set
            {
                //no op
            }
        }

        public override string _requestId { get; set; }
        public override string dataProviderName { get; set; }

        public K8SELogAnalyticsClient(string requestId)
        {
            creds = K8SELogAnalyticsTokenService.Instance.getCreds();
            workspaceId = K8SELogAnalyticsTokenService.Instance.getWorkspaceId();
            _requestId = requestId;
            dataProviderName = "K8SELogAnalyticsDataProvider";
        }
    }
}

