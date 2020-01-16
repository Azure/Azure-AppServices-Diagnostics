using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json.Linq;

namespace Diagnostics.DataProviders
{
    public interface ISupportObserverDataProvider : IMetadataProvider
    {
        Task<dynamic> GetSite(string siteName);

        Task<dynamic> GetSite(string stampName, string siteName);

        Task<dynamic> GetSite(string stampName, string siteName, string slotName);

        Task<JArray> GetAdminSitesAsync(string siteName);

        Task<JArray> GetAdminSitesAsync(string siteName, string stampName);

        Task<string> GetSiteResourceGroupNameAsync(string siteName);

        Task<dynamic> GetSitesInResourceGroupAsync(string subscriptionName, string resourceGroupName);

        Task<dynamic> GetServerFarmsInResourceGroupAsync(string subscriptionName, string resourceGroupName);

        Task<dynamic> GetCertificatesInResourceGroupAsync(string subscriptionName, string resourceGroupName);

        Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName);

        Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm);

        Task<string> GetSiteWebSpaceNameAsync(string subscriptionId, string siteName);

        Task<dynamic> GetSitesInServerFarmAsync(string subscriptionId, string serverFarmName);

        Task<dynamic> GetHostingEnvironmentPostBody(string hostingEnvironmentName);

        Task<string> GetStampName(string subscriptionId, string resourceGroupName, string siteName);

        Task<dynamic> GetHostNames(string stampName, string siteName);

        Task<dynamic> GetSitePostBody(string stampName, string siteName);

        Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName);

        Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName, string slotName);

        Task<dynamic> GetResource(string wawsObserverUrl);

        Task<DataTable> ExecuteSqlQueryAsync(string cloudServiceName, string query);
    }
}
