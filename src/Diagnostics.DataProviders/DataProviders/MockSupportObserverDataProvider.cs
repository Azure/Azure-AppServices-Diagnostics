using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Use this class for testing purposes
    /// </summary>
    public class MockSupportObserverDataProvider : DiagnosticDataProvider, ISupportObserverDataProvider
    {
        public MockSupportObserverDataProvider(OperationDataCache cache) : base(cache)
        {

        }

        public Task<JObject> GetAdminSitesByHostName(string stampName, string[] hostNames)
        {
            throw new NotImplementedException();
        }

        public Task<JObject> GetAdminSitesBySiteName(string stampName, string siteName)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<object>> GetAppServiceEnvironmentDeployments(string hostingEnvironmentName)
        {
            throw new NotImplementedException();
        }

        public Task<JObject> GetAppServiceEnvironmentDetails(string hostingEnvironmentName)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Dictionary<string, string>>> GetCertificatesInResourceGroup(string subscriptionName, string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string siteName)
        {
            return GetRuntimeSiteSlotMap(null, siteName);
        }

        public Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName)
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

        public Task<IEnumerable<Dictionary<string, string>>> GetServerFarmsInResourceGroup(string subscriptionName, string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm)
        {
            throw new NotImplementedException();
        }

        public async Task<dynamic> GetSite(string siteName)
        {
            return await GetSiteInternal();
        }

        public async Task<dynamic> GetSite(string stampName, string siteName)
        {
            return await GetSiteInternal();
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

        public Task<string> GetSiteResourceGroupName(string siteName)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Dictionary<string, string>>> GetSitesInResourceGroup(string subscriptionName, string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Dictionary<string, string>>> GetSitesInServerFarm(string subscriptionId, string serverFarmName)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetSiteWebSpaceName(string subscriptionId, string siteName)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetStorageVolumeForSite(string stampName, string siteName)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName)
        {
            throw new NotImplementedException();
        }
    }
}
