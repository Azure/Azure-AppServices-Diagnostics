using Diagnostics.RuntimeHost.Models;
using System.Collections.Immutable;

namespace Diagnostics.RuntimeHost.Services.CacheService
{
    /// <summary>
    /// Gist cache service interface.
    /// </summary>
    public interface IGistCacheService : ICache<string, GistEntry>
    {
        /// <summary>
        /// Get all references.
        /// </summary>
        /// <returns>Reference dictionary.</returns>
        IImmutableDictionary<string, string> GetAllReferences();
    }
}
