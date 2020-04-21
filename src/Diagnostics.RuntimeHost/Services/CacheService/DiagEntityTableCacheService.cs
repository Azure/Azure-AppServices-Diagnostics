using System.Collections.Generic;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;
using System.Collections.Concurrent;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Services.StorageService;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Diagnostics.RuntimeHost.Models;
using System.Linq;

namespace Diagnostics.RuntimeHost.Services.CacheService
{
    public class DiagEntityTableCacheService : IDiagEntityTableCacheService
    {
        private ConcurrentDictionary<string, List<DiagEntity>> cache;

        private IStorageService storageService;

        public DiagEntityTableCacheService(IStorageService service)
        {
            cache = new ConcurrentDictionary<string, List<DiagEntity>>();
            storageService = service;
        }
        public void AddOrUpdate(string key, List<DiagEntity> value)
        {
            cache.AddOrUpdate(key, value, (existingKey, existingValue) => value);
        }

        public bool ContainsKey(string key)
        {
            return cache.ContainsKey(key);
        }

        public IEnumerable<List<DiagEntity>> GetAll()
        {
            return cache.Values;
        }

        public bool TryGetValue(string key, out List<DiagEntity> value)
        {
            return cache.TryGetValue(key, out value);
        }

        public bool TryRemoveValue(string key, out List<DiagEntity> value)
        {
            return cache.TryRemove(key, out value);
        }

        public async Task<List<DiagEntity>> GetEntityListByType<TResource>(RuntimeContext<TResource> context, string entityType = null)
          where TResource : IResource
        {
            entityType = entityType == null ? "Detector" : entityType;

            var result = new List<DiagEntity>();

            while (!TryGetValue(entityType, out result))
            {
                var tableResult = await storageService.GetEntitiesByPartitionkey(entityType);
                AddOrUpdate(entityType, tableResult);            
            }

            result = result.Where(tableEntity => context.OperationContext.Resource.IsApplicable(tableEntity)).OrderBy(tableRow => tableRow.DetectorName).ToList();
            return result;
        }

        public bool IsStorageAsSourceEnabled()
        {
            return storageService.GetStorageFlag();
        }
    }
}
