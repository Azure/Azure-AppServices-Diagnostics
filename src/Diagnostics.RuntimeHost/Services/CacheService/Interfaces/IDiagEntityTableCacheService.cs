using System.Collections.Generic;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Diagnostics.RuntimeHost.Models;
namespace Diagnostics.RuntimeHost.Services.CacheService.Interfaces
{
    public interface IDiagEntityTableCacheService: ICache<string, List<DiagEntity>>
    {
        Task<List<DiagEntity>> GetEntityListByType<TResource>(RuntimeContext<TResource> context, string entityType = null) where TResource : IResource;

        bool IsStorageAsSourceEnabled();
    }
}
