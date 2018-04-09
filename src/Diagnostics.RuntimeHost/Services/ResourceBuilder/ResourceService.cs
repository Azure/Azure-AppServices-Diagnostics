using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services
{
    public interface IResourceService
    {
        Task<SiteResource> GetSite(string subscriptionId, string resourceGroup, string siteName, IEnumerable<string> hostNames, string stampName, DateTime startTime, DateTime endTime, string requestId = null);
    }

    public class ResourceService : IResourceService
    {
        private ITenantIdService _tenantIdService;

        public ResourceService(ITenantIdService tenantIdService)
        {
            _tenantIdService = tenantIdService;
        }

        public async Task<SiteResource> GetSite(string subscriptionId, string resourceGroup, string siteName, IEnumerable<string> hostNames, string stampName, DateTime startTime, DateTime endTime, string requestId = null)
        {
            SiteResource resource = new SiteResource()
            {
                SubscriptionId = subscriptionId,
                ResourceGroup = resourceGroup,
                SiteName = siteName,
                HostNames = hostNames,
                Stamp = stampName
            };

            resource.TenantIdList = await _tenantIdService.GetTenantIdForStamp(stampName, startTime, endTime, requestId);
            return resource;
        }
    }
}
