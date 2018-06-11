using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface IGeoMasterDataProvider
    {
        Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name);

        Task<IDictionary<string, string[]>> GetStickySlotSettingNames(string subscriptionId, string resourceGroupName, string name);

        Task<VnetValidationRespone> VerifyHostingEnvironmentVnet(string subscriptionId, string vnetResourceGroup, string vnetName, string vnetSubnetName, CancellationToken cancellationToken = default(CancellationToken));

        Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name);

        Task<T> MakeHttpGetRequest<T>(string subscriptionId, string resourceGroupName, string name, string path = "");
    }
}
