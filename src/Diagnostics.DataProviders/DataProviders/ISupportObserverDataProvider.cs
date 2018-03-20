using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface ISupportObserverDataProvider
    {
        Task<IEnumerable<string>> GetSiteHostNames(string siteName);
        Task<string> GetSiteResourceGroupName(string siteName);
        Task<IEnumerable<Dictionary<string, string>>> GetSitesInResourceGroup(string subscriptionName, string resourceGroupName);
        Task<IEnumerable<Dictionary<string, string>>> GetServerFarmsInResourceGroup(string subscriptionName, string resourceGroupName);
        Task<IEnumerable<Dictionary<string, string>>> GetCertificatesInResourceGroup(string subscriptionName, string resourceGroupName);
        Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName);

        Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm);
        Task<string> GetSiteWebSpaceName(string subscriptionId, string siteName);
        Task<IEnumerable<Dictionary<string, string>>> GetSitesInServerFarm(string subscriptionId, string serverFarmName);
        Task<JObject> GetAppServiceEnvironmentDetails(string hostingEnvironmentName);
        Task<IEnumerable<object>> GetAppServiceEnvironmentDeployments(string hostingEnvironmentName);
        Task<JObject> GetAdminSitesBySiteName(string stampName, string siteName);
        Task<JObject> GetAdminSitesByHostName(string stampName, string[] hostNames);
        Task<string> GetStorageVolumeForSite(string stampName, string siteName);
        Task<JObject> GetRuntimeSiteSlotMap(string siteName);
        Task<JObject> GetRuntimeSiteSlotMap(string stampName, string siteName);
    }
}
