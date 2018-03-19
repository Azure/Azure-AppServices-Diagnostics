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

    public class InvokerCacheService : ICache<string, EntityInvoker>
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
    }
}
