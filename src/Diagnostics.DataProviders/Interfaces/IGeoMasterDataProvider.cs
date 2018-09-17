using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface IGeoMasterDataProvider: IMetadataProvider
    {
        Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name);

        Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name, string slotName);

        Task<IDictionary<string, string[]>> GetStickySlotSettingNames(string subscriptionId, string resourceGroupName, string name);

        Task<VnetValidationRespone> VerifyHostingEnvironmentVnet(string subscriptionId, string vnetResourceGroup, string vnetName, string vnetSubnetName, CancellationToken cancellationToken = default(CancellationToken));

        Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name);

        Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name, string slotName);

        Task<T> MakeHttpGetRequest<T>(string subscriptionId, string resourceGroupName, string name, string path = "");

        Task<T> MakeHttpGetRequest<T>(string subscriptionId, string resourceGroupName, string name, string slotName = GeoMasterConstants.ProductionSlot, string path = "");

        Task<T> MakeHttpGetRequestWithFullPath<T>(string fullPath, string queryString = "", string apiVersion = GeoMasterConstants.August2016Version);

        Task<string> GetLinuxContainerLogs(string subscriptionId, string resourceGroupName, string name, string slotName);
    }
}
