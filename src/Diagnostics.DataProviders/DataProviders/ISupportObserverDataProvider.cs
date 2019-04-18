using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface ISupportObserverDataProvider:IMetadataProvider
    {
        Task<dynamic> GetSite(string siteName);
        Task<dynamic> GetSite(string stampName, string siteName);
        Task<dynamic> GetSite(string stampName, string siteName, string slotName);
        Task<string> GetSiteResourceGroupNameAsync(string siteName);
        Task<dynamic> GetSitesInResourceGroupAsync(string subscriptionName, string resourceGroupName);
        Task<dynamic> GetServerFarmsInResourceGroupAsync(string subscriptionName, string resourceGroupName);
        Task<dynamic> GetCertificatesInResourceGroupAsync(string subscriptionName, string resourceGroupName);
        Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName);

        Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm);
        Task<string> GetSiteWebSpaceNameAsync(string subscriptionId, string siteName);
        Task<dynamic> GetSitesInServerFarmAsync(string subscriptionId, string serverFarmName);
        Task<JObject> GetAppServiceEnvironmentDetailsAsync(string hostingEnvironmentName);
        Task<dynamic> GetHostingEnvironmentPostBody(string hostingEnvironmentName);
        Task<IEnumerable<object>> GetAppServiceEnvironmentDeploymentsAsync(string hostingEnvironmentName);
        Task<string> GetStampName(string subscriptionId, string resourceGroupName, string siteName);
        Task<dynamic> GetHostNames(string stampName, string siteName);
        Task<dynamic> GetSitePostBody(string stampName, string siteName);
        Task<JObject> GetAdminSitesBySiteNameAsync(string stampName, string siteName);
        Task<JObject> GetAdminSitesByHostNameAsync(string stampName, string[] hostNames);
        Task<string> GetStorageVolumeForSiteAsync(string stampName, string siteName);
        Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string siteName);
        Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName);
        Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName, string slotName);
        Task<dynamic> GetResource(string wawsObserverUrl);
    }
}
