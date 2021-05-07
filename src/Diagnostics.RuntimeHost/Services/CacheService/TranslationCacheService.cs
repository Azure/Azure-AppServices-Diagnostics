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
        private ConcurrentDictionary<Tuple<string, string>, List<string>> _cache;

        public TranslationCacheService()
        {
            _cache = new ConcurrentDictionary<Tuple<string, string>, List<string>>();
        }

        public void AddOrUpdate(Tuple<string, string> key, List<string> value)
        {
            _cache.AddOrUpdate(key, value, (existingKey, existingValue) => value);
        }

        public bool ContainsKey(Tuple<string, string> key)
        {
            return _cache.ContainsKey(key);
        }

        public IEnumerable<List<string>> GetAll()
        {
            return _cache.Values;
        }

        public bool TryGetValue(Tuple<string, string> key, out List<string> value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public bool TryRemoveValue(Tuple<string, string> key, out List<string> value)
        {
            return _cache.TryRemove(key, out value);
        }
    }
}
