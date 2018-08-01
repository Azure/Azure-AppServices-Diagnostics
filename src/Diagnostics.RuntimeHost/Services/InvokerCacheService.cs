using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.Scripts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services
{
    public interface ICache<K, V>
    {
        void AddOrUpdate(K key, V value);

        bool TryGetValue(K key, out V value);

        bool TryRemoveValue(K key, out V value);

        IEnumerable<V> GetAll();
    }

    public interface IInvokerCacheService : ICache<string, EntityInvoker>
    {
        EntityInvoker GetDetectorInvoker<TResource>(string detectorId, OperationContext<TResource> context)
            where TResource : IResource;

        IEnumerable<EntityInvoker> GetDetectorInvokerList<TResource>(OperationContext<TResource> context)
            where TResource : IResource;

        EntityInvoker GetSystemInvoker(string invokerId);

        IEnumerable<EntityInvoker> GetSystemInvokerList<TResource>(OperationContext<TResource> context)
            where TResource : IResource;
    }

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

        public IEnumerable<EntityInvoker> GetDetectorInvokerList<TResource>(OperationContext<TResource> context)
            where TResource : IResource
        {
            IEnumerable<EntityInvoker> list = GetAll();

            if (list == null || !list.Any()) return list;

            list = list.Where(item => ((item.SystemFilter == null) && (item.ResourceFilter != null) && (item.ResourceFilter.ResourceType & context.Resource.ResourceType) > 0) && (context.IsInternalCall || !item.ResourceFilter.InternalOnly));
            List<EntityInvoker> filteredList = new List<EntityInvoker>();
            list.ToList().ForEach(item =>
            {
                if (context.Resource.IsApplicable(item.ResourceFilter))
                {
                    filteredList.Add(item);
                }
            });

            return filteredList.OrderBy(p => p.EntryPointDefinitionAttribute.Name);
        }

        public EntityInvoker GetDetectorInvoker<TResource>(string detectorId, OperationContext<TResource> context)
            where TResource : IResource
        {
            if (!TryGetValue(detectorId, out EntityInvoker invoker) || (!context.IsInternalCall && invoker.ResourceFilter.InternalOnly) || invoker.SystemFilter != null)
            {
                return null;
            }

            if (context.Resource.IsApplicable(invoker.ResourceFilter))
            {
                return invoker;
            }

            return null;
        }

        public IEnumerable<EntityInvoker> GetSystemInvokerList<TResource>(OperationContext<TResource> context)
    where TResource : IResource
        {
            IEnumerable<EntityInvoker> list = GetAll();

            if (list == null || !list.Any()) return list;

            list = list.Where(item => (context.IsInternalCall && item.SystemFilter != null));

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
