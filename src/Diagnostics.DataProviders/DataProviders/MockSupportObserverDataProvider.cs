using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Use this class for testing purposes
    /// </summary>
    public class MockSupportObserverDataProvider : SupportObserverDataProviderBase
    {
        public MockSupportObserverDataProvider(OperationDataCache cache, SupportObserverDataProviderConfiguration configuration) : base(cache, configuration)
        {

        }

        public override Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string siteName)
        {
            return GetRuntimeSiteSlotMap(null, siteName);
        }

        public override Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName)
        {
            if (string.IsNullOrWhiteSpace(siteName))
            {
                throw new ArgumentNullException("siteName");
            }

            if (string.IsNullOrWhiteSpace(stampName))
            {
                throw new ArgumentNullException("stampName");
            }

            var mock = new Dictionary<string, List<RuntimeSitenameTimeRange>>();

            switch (siteName.ToLower())
            {
                case "my-api":
                    mock.Add("production", new List<RuntimeSitenameTimeRange> { new RuntimeSitenameTimeRange { RuntimeSitename = "my-api;", StartTime = DateTime.UtcNow.AddDays(-5), EndTime = DateTime.UtcNow } });
                    mock.Add("staging", new List<RuntimeSitenameTimeRange> { new RuntimeSitenameTimeRange { RuntimeSitename = "my-api__a88nf", StartTime = DateTime.UtcNow.AddDays(-5), EndTime = DateTime.UtcNow } });
                    mock.Add("testing", new List<RuntimeSitenameTimeRange> { new RuntimeSitenameTimeRange { RuntimeSitename = "my-api__v85ae", StartTime = DateTime.UtcNow.AddDays(-5), EndTime = DateTime.UtcNow } });
                    break;
                default:
                    break;
            }

            return Task.FromResult(mock);
        }

        public override async Task<dynamic> GetSite(string siteName)
        {
            throw new NotImplementedException();
        }

        public override async Task<dynamic> GetSite(string stampName, string siteName)
        {
            return await GetSiteInternal();
        }

        public override async Task<string> GetStampName(string subscriptionId, string resourceGroupName, string siteName)
        {
            throw new NotImplementedException();
        }

        public override async Task<dynamic> GetHostNames(string stampName, string siteName)
        {
            throw new NotImplementedException();
        }

        public override Task<dynamic> GetSitePostBody(string stampName, string siteName)
        {
            throw new NotImplementedException();
        }
        public override Task<dynamic> GetHostingEnvironmentPostBody(string hostingEnvironmentName)
        {
            throw new NotImplementedException();
        }

        private Task<dynamic> GetSiteInternal()
        {
            var siteData = @"
[
  {
    ""sku"": ""Premium"",
    ""backups"": [
      {
        ""id"": 1,
        ""name"": ""my-api;_201611282122147173"",
        ""blob_name"": ""my-api;_201611282122147173.zip"",
        ""status"": 1,
        ""created_time"": ""2016-11-28T21: 22: 16.2544605"",
        ""started_time_stamp"": ""2016-11-28T21: 22: 19.2148232"",
        ""finished_time_stamp"": ""2016-11-28T21: 33: 40.5215806"",
        ""log"": ""Thewebsite+databasesizeexceedsthe10GBlimitforbackups.Yourcontentsizeis10GB."",
        ""correlation_id"": ""f786719b-6d62-4993-b94e-d862dc9c2070""
      }
    ]
  }
]";
            return Task.FromResult(JsonConvert.DeserializeObject(siteData));
        }

        public override Task<string> GetSiteResourceGroupNameAsync(string siteName)
        {
            throw new NotImplementedException();
        }

        public override Task<dynamic> GetSitesInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public override Task<dynamic> GetServerFarmsInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public override Task<dynamic> GetCertificatesInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public override Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName)
        {
            throw new NotImplementedException();
        }

        public override Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm)
        {
            throw new NotImplementedException();
        }

        public override Task<string> GetSiteWebSpaceNameAsync(string subscriptionId, string siteName)
        {
            throw new NotImplementedException();
        }

        public override Task<dynamic> GetSitesInServerFarmAsync(string subscriptionId, string serverFarmName)
        {
            throw new NotImplementedException();
        }

        public override Task<JObject> GetAppServiceEnvironmentDetailsAsync(string hostingEnvironmentName)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<object>> GetAppServiceEnvironmentDeploymentsAsync(string hostingEnvironmentName)
        {
            throw new NotImplementedException();
        }

        public override Task<JObject> GetAdminSitesBySiteNameAsync(string stampName, string siteName)
        {
            throw new NotImplementedException();
        }

        public override Task<JObject> GetAdminSitesByHostNameAsync(string stampName, string[] hostNames)
        {
            throw new NotImplementedException();
        }

        public override Task<string> GetStorageVolumeForSiteAsync(string stampName, string siteName)
        {
            throw new NotImplementedException();
        }

        public override HttpClient GetObserverClient()
        {
            return new MockHttpClient();
        }

        private class MockHttpClient : HttpClient
        {
            public MockHttpClient()
            {
                BaseAddress = new Uri("https://mock-wawsobserver-prod.azurewebsites.net/api/");
                DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                response.Content = new StringContent(MockJsonResponse(request.RequestUri.OriginalString));
                return Task.FromResult(response);
            }

            private string MockJsonResponse(string pathAndQuery)
            {
                switch (pathAndQuery)
                {
                    case "stamps/stamp123/sites/site123":
                        return "{\"stamp\":\"stamp123\",\"siteName\":\"site123\"}";
                    case "certificates/123456789":
                        return "{\"type\":\"IPSSL\"}";
                    case "subscriptions/1111-2222-3333-4444-5555/domains":
                        return "[{\"name\":\"foo_bar.com\"},{\"name\":\"foo_bar.net\"}]";
                    case "minienvironments/my-ase":
                        return "{\"hostingEnvironmentType\":\"ASEv2\"}";
                    case "stamps/waws-prod-mock1-001/storagevolumes/volume-19":
                        return "{\"name\":\"volume-19\"}";
                    default:
                        return null;
                }
            }
        }
    }
}
