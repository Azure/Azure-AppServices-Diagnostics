using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.RuntimeHost.Models;
using Octokit;

namespace Diagnostics.RuntimeHost.Services.CacheService
{
    public class KustoMappingsCacheService : IKustoMappingsCacheService
    {
        private ConcurrentDictionary<string, Table> _cache;

        public KustoMappingsCacheService()
        {
            _cache = new ConcurrentDictionary<string, Table>(StringComparer.CurrentCultureIgnoreCase);
        }

        public void AddOrUpdate(string key, Table value)
        {
            _cache.AddOrUpdate(key, value, (existingKey, existingValue) => value);
        }

        public bool ContainsKey(string key)
        {
            return _cache.ContainsKey(key);
        }

        public IEnumerable<Table> GetAll()
        {
            return _cache.Values;
        }

        public bool TryGetValue(string key, out Table value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public bool TryRemoveValue(string key, out Table value)
        {
            return _cache.TryRemove(key, out value);
        }
    }
}
