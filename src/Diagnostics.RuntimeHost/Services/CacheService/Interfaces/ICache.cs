using System.Collections.Generic;

namespace Diagnostics.RuntimeHost.Services.CacheService
{
    /// <summary>
    /// Interface for cache.
    /// </summary>
    /// <typeparam name="K">Cache key.</typeparam>
    /// <typeparam name="V">Cache value.</typeparam>
    public interface ICache<K, V>
    {
        /// <summary>
        /// Add or update cache entry.
        /// </summary>
        /// <param name="key">Entry key.</param>
        /// <param name="value">Entry value.</param>
        void AddOrUpdate(K key, V value);

        /// <summary>
        /// Try to get the cache value.
        /// </summary>
        /// <param name="key">Entry key.</param>
        /// <param name="value">Entry value.</param>
        /// <returns>A value to indicate get operation success or not.</returns>
        bool TryGetValue(K key, out V value);

        /// <summary>
        /// Determines whether the cache has the specified key.
        /// </summary>
        /// <param name="key">Entry key.</param>
        /// <returns>A value to indicate get operation success or not.</returns>
        bool ContainsKey(K key);

        /// <summary>
        /// Try to remove the entry.
        /// </summary>
        /// <param name="key">Entry key.</param>
        /// <param name="value">Entry value.</param>
        /// <returns>A value to indicate remove operation success or not.</returns>
        bool TryRemoveValue(K key, out V value);

        /// <summary>
        /// Get all value.
        /// </summary>
        /// <returns>All value.</returns>
        IEnumerable<V> GetAll();
    }
}
