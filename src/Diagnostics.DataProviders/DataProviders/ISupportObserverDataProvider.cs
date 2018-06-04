using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface ISupportObserverDataProvider
    {
        Task<dynamic> GetSite(string siteName);
        Task<dynamic> GetSite(string stampName, string siteName);
        Task<string> GetSiteResourceGroupName(string siteName);
        Task<dynamic> GetSitesInResourceGroup(string subscriptionName, string resourceGroupName);
        Task<dynamic> GetServerFarmsInResourceGroup(string subscriptionName, string resourceGroupName);
        Task<dynamic> GetCertificatesInResourceGroup(string subscriptionName, string resourceGroupName);
        Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName);

        Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm);
        Task<string> GetSiteWebSpaceName(string subscriptionId, string siteName);
        Task<dynamic> GetSitesInServerFarm(string subscriptionId, string serverFarmName);
        Task<JObject> GetAppServiceEnvironmentDetails(string hostingEnvironmentName);
        Task<IEnumerable<object>> GetAppServiceEnvironmentDeployments(string hostingEnvironmentName);
        Task<JObject> GetAdminSitesBySiteName(string stampName, string siteName);
        Task<JObject> GetAdminSitesByHostName(string stampName, string[] hostNames);
        Task<string> GetStorageVolumeForSite(string stampName, string siteName);
        Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string siteName);
        Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName);
        Task<dynamic> GetResource(string wawsObserverUrl);
    }
}
