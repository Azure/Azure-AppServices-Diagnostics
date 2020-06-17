using System.Collections.Generic;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;

namespace Diagnostics.RuntimeHost.Services.CacheService
{
    public class NullableDiagEntityTableCacheService : IDiagEntityTableCacheService
    {
        public void AddOrUpdate(string key, List<DiagEntity> value)
        {
        }

        public bool ContainsKey(string key)
        {
            return false;
        }

        public IEnumerable<List<DiagEntity>> GetAll()
        {
            return (IEnumerable<List<DiagEntity>>)new List<DiagEntity>();
        }

        public Task<List<DiagEntity>> GetEntityListByType<TResource>(RuntimeContext<TResource> context, string entityType = null) where TResource : IResource
        {
            return Task.FromResult(new List<DiagEntity>());
        }

        public bool IsStorageAsSourceEnabled()
        {
            return false;
        }

        public bool TryGetValue(string key, out List<DiagEntity> value)
        {
            value = null;
            return false;
        }

        public bool TryRemoveValue(string key, out List<DiagEntity> value)
        {
            value = null;
            return false;
        }
    }
}
