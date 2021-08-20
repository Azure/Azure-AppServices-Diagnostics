using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Concurrent;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;
namespace Diagnostics.RuntimeHost.Services.CacheService
{
    public class GistScriptCache : IGistScriptCache
    {
        private ConcurrentDictionary<string, string> cache;

        public GistScriptCache()
        {
            cache = new ConcurrentDictionary<string, string>();
        }
        public void AddOrUpdate(string key, string value)
        {
            cache.AddOrUpdate(key, value, (existingKey, existingValue) => value);
        }

        public bool ContainsKey(string key)
        {
            return cache.ContainsKey(key);
        }

        public IEnumerable<string> GetAll()
        {
            return cache.Values;
        }

        bool ICache<string, string>.TryGetValue(string key, out string value)
        {
            return cache.TryGetValue(key, out value);
        }

        bool ICache<string, string>.TryRemoveValue(string key, out string value)
        {
            return cache.TryRemove(key, out value);
        }
    }
}
