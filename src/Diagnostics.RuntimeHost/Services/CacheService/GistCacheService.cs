using Diagnostics.RuntimeHost.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Diagnostics.RuntimeHost.Services.CacheService
{
    /// <summary>
    /// Gist cache service.
    /// </summary>
    public class GistCacheService : IGistCacheService
    {
        private ConcurrentDictionary<string, GistEntry> _collection;
        private ConcurrentDictionary<string, string> _reference;

        /// <summary>
        /// Initialize a new instance of <see cref="GistCacheService"/> class.
        /// </summary>
        public GistCacheService()
        {
            _collection = new ConcurrentDictionary<string, GistEntry>();
            _reference = new ConcurrentDictionary<string, string>();
        }

        /// <summary>
        /// Add or update cache.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="value">Cache value.</param>
        public void AddOrUpdate(string key, GistEntry value)
        {
            _collection.AddOrUpdate(key.ToLower(), value, (existingKey, oldValue) => value);
            _reference.AddOrUpdate(value.Id, value.CodeString, (existingKey, oldValue) => value.CodeString);
        }

        /// <summary>
        /// Get all values.
        /// </summary>
        /// <returns>All values.</returns>
        public IEnumerable<GistEntry> GetAll()
        {
            return _collection.Values;
        }

        /// <summary>
        /// Get all references.
        /// </summary>
        /// <returns>Reference dictionary.</returns>
        public IImmutableDictionary<string, string> GetAllReferences()
        {
            return _reference.ToImmutableDictionary();
        }

        /// <summary>
        /// Try to get the cache value.
        /// </summary>
        /// <param name="key">Entry key.</param>
        /// <param name="value">Entry value.</param>
        /// <returns>A value to indicate get operation success or not.</returns>
        public bool TryGetValue(string key, out GistEntry value)
        {
            return _collection.TryGetValue(key.ToLower(), out value);
        }

        /// <summary>
        /// Try to remove the entry.
        /// </summary>
        /// <param name="key">Entry key.</param>
        /// <param name="value">Entry value.</param>
        /// <returns>A value to indicate remove operation success or not.</returns>
        public bool TryRemoveValue(string key, out GistEntry value)
        {
            if (_collection.TryRemove(key.ToLower(), out value))
            {
                _reference.TryRemove(value.Id, out var code);
                return true;
            }

            return false;
        }
    }
}
