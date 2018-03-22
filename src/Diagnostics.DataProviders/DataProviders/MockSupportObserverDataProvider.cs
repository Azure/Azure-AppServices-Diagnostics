using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
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
                case "thor-api":
                    mock.Add("production", new List<RuntimeSitenameTimeRange> { new RuntimeSitenameTimeRange { RuntimeSitename = "thor-api", StartTime = DateTime.UtcNow.AddDays(-5), EndTime = DateTime.UtcNow } });
                    mock.Add("staging", new List<RuntimeSitenameTimeRange> { new RuntimeSitenameTimeRange { RuntimeSitename = "thor-api__a88nf", StartTime = DateTime.UtcNow.AddDays(-5), EndTime = DateTime.UtcNow } });
                    mock.Add("testing", new List<RuntimeSitenameTimeRange> { new RuntimeSitenameTimeRange { RuntimeSitename = "thor-api__v85ae", StartTime = DateTime.UtcNow.AddDays(-5), EndTime = DateTime.UtcNow } });
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

        public Task<dynamic> GetSite(string siteName)
        {
            throw new NotImplementedException();
        }

        public Task<dynamic> GetSite(string stampName, string siteName)
        {
            throw new NotImplementedException();
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
