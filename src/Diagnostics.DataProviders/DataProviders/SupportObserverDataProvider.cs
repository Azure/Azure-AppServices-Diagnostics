using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Diagnostics.DataProviders
{
    public class SupportObserverDataProvider : SupportObserverDataProviderBase
    {
        private object _lockObject = new object();

        public SupportObserverDataProvider(OperationDataCache cache, SupportObserverDataProviderConfiguration configuration) : base(cache, configuration)
        {
        }

        public override Task<JObject> GetAdminSitesByHostName(string stampName, string[] hostNames)
        {
            throw new NotImplementedException();
        }

        public override Task<JObject> GetAdminSitesBySiteName(string stampName, string siteName)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<object>> GetAppServiceEnvironmentDeployments(string hostingEnvironmentName)
        {
            throw new NotImplementedException();
        }

        public override Task<JObject> GetAppServiceEnvironmentDetails(string hostingEnvironmentName)
        {
            throw new NotImplementedException();
        }

        public override async Task<dynamic> GetCertificatesInResourceGroup(string subscriptionName, string resourceGroupName)
        {
            var result = await Get($"subscriptions/{subscriptionName}/resourceGroups/{resourceGroupName}/certificates");
            return JsonConvert.DeserializeObject(result);
        }

        public override async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string siteName)
        {
            return await GetRuntimeSiteSlotMap(null, siteName);
        }

        public override async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName)
        {
            return await GetRuntimeSiteSlotMapInternal(stampName, siteName);
        }

        private async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMapInternal(string stampName, string siteName)
        {
            var result = await Get(string.IsNullOrWhiteSpace(stampName) ? $"/sites/{siteName}/runtimesiteslotmap" : $"stamp/{stampName}/sites/{siteName}/runtimesiteslotmap");
            var slotTimeRangeCaseSensitiveDictionary = JsonConvert.DeserializeObject<Dictionary<string, List<RuntimeSitenameTimeRange>>>(result);
            var slotTimeRange = new Dictionary<string, List<RuntimeSitenameTimeRange>>(slotTimeRangeCaseSensitiveDictionary, StringComparer.CurrentCultureIgnoreCase);
            return slotTimeRange;
        }

        public override async Task<dynamic> GetServerFarmsInResourceGroup(string subscriptionName, string resourceGroupName)
        {
            var result = await Get($"subscriptions/{subscriptionName}/resourceGroups/{resourceGroupName}/serverfarms");
            return JsonConvert.DeserializeObject(result);
        }

        public override async Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm)
        {
            return await Get($"subscriptionid/{subscriptionId}/serverfarm/{serverFarm}/webspacename");
        }

        public override async Task<string> GetSiteResourceGroupName(string siteName)
        {
            return await Get($"sites/{siteName}/resourcegroupname");
        }

        public override async Task<dynamic> GetSitesInResourceGroup(string subscriptionName, string resourceGroupName)
        {
            var sitesResult = await Get($"subscriptions/{subscriptionName}/resourceGroups/{resourceGroupName}");
            return JsonConvert.DeserializeObject(sitesResult);
        }

        public override async Task<dynamic> GetSitesInServerFarm(string subscriptionId, string serverFarmName)
        {
            var sitesResult = await Get($"subscriptionid/{subscriptionId}/serverfarm/{serverFarmName}/sites");
            return JsonConvert.DeserializeObject(sitesResult);
        }

        public override async Task<string> GetSiteWebSpaceName(string subscriptionId, string siteName)
        {
            return await Get($"subscriptionid/{subscriptionId}/sitename/{siteName}/webspacename");
        }

        public override Task<string> GetStorageVolumeForSite(string stampName, string siteName)
        {
            throw new NotImplementedException();
        }

        public override async Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName)
        {
            return await Get($"subscriptionid/{subscriptionId}/webspacename/{webSpaceName}/resourcegroupname");
        }

        public override async Task<dynamic> GetSite(string siteName)
        {
            var response = await GetObserverResource($"sites/{siteName}");
            var siteObject = JsonConvert.DeserializeObject(response);
            return siteObject;
        }

        public override async Task<dynamic> GetSite(string stampName, string siteName)
        {
            var response = await GetObserverResource($"stamps/{stampName}/sites/{siteName}");
            var siteObject = JsonConvert.DeserializeObject(response);
            return siteObject;
        }

        public override HttpClient GetObserverClient()
        {
            return new HttpClient();
        }

        /// <summary>
        /// Temporary solution
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        private async Task<string> Get(string path)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://support-bay-api.azurewebsites.net/observer/{path}?api-version=2.0");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _configuration.GetAccessToken(_configuration.RuntimeSiteSlotMapResourceUri));
            var response = await GetObserverClient().SendAsync(request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
    }
}
