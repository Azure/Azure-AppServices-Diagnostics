using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.Scripts;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Diagnostics.RuntimeHost.Services.CacheService
{
    /// <summary>
    /// Invoker cache service.
    /// </summary>
    public class InvokerCacheService : IInvokerCacheService
    {
        private ConcurrentDictionary<string, EntityInvoker> _collection;

        public InvokerCacheService()
        {
            _collection = new ConcurrentDictionary<string, EntityInvoker>();
        }

        public void AddOrUpdate(string key, EntityInvoker value)
        {
            _collection.AddOrUpdate(key.ToLower(), value, (existingKey, oldValue) => value);
        }

        public IEnumerable<EntityInvoker> GetAll()
        {
            return _collection.Values;
        }

        public bool TryRemoveValue(string key, out EntityInvoker value)
        {
            return _collection.TryRemove(key.ToLower(), out value);
        }

        public bool TryGetValue(string key, out EntityInvoker value)
        {
            return _collection.TryGetValue(key.ToLower(), out value);
        }

        public IEnumerable<EntityInvoker> GetEntityInvokerList<TResource>(RuntimeContext<TResource> context)
            where TResource : IResource
        {
            IEnumerable<EntityInvoker> list = GetAll();

            if (list == null || !list.Any()) return list;

            list = list.Where(item => ((item.SystemFilter == null) && (item.ResourceFilter != null) && (item.ResourceFilter.ResourceType & context.OperationContext.Resource.ResourceType) > 0) && (context.ClientIsInternal || !item.ResourceFilter.InternalOnly));
            List<EntityInvoker> filteredList = new List<EntityInvoker>();
            list.ToList().ForEach(item =>
            {
                if (context.OperationContext.Resource.IsApplicable(item.ResourceFilter))
                {
                    filteredList.Add(item);
                }
            });

            return filteredList.OrderBy(p => p.EntryPointDefinitionAttribute.Name);
        }

        public EntityInvoker GetEntityInvoker<TResource>(string detectorId, RuntimeContext<TResource> context)
            where TResource : IResource
        {
            if (!TryGetValue(detectorId, out EntityInvoker invoker) || invoker.SystemFilter != null || invoker.ResourceFilter == null || (!context.ClientIsInternal && invoker.ResourceFilter.InternalOnly))
            {
                return null;
            }

            if (context.OperationContext.Resource.IsApplicable(invoker.ResourceFilter))
            {
                return invoker;
            }

            return null;
        }

        public IEnumerable<EntityInvoker> GetSystemInvokerList<TResource>(RuntimeContext<TResource> context)
            where TResource : IResource
        {
            IEnumerable<EntityInvoker> list = GetAll();

            if (list == null || !list.Any()) return list;

            list = list.Where(item => (context.ClientIsInternal && item.SystemFilter != null));

            return list.OrderBy(p => p.EntryPointDefinitionAttribute.Name);
        }

        public EntityInvoker GetSystemInvoker(string invokerId)
        {
            if (!TryGetValue(invokerId, out EntityInvoker invoker) || invoker.SystemFilter == null || invoker.ResourceFilter != null)
            {
                return null;
            }

            return invoker;
        }
    }
}
