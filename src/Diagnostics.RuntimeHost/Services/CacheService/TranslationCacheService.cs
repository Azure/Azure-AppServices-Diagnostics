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
    public class TranslationCacheService : ITranslationCacheService
    {
        private ConcurrentDictionary<string, string> _cache;

        public TranslationCacheService()
        {
            _cache = new ConcurrentDictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
        }

        public void AddOrUpdate(string key, string value)
        {
            _cache.AddOrUpdate(key, value, (existingKey, existingValue) => value);
        }

        public bool ContainsKey(string key)
        {
            return _cache.ContainsKey(key);
        }

        public IEnumerable<string> GetAll()
        {
            return _cache.Values;
        }

        public bool TryGetValue(string key, out string value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public bool TryRemoveValue(string key, out string value)
        {
            return _cache.TryRemove(key, out value);
        }
    }
}
