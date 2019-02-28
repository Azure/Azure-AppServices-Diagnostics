using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.Scripts;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Diagnostics.RuntimeHost.Services.CacheService
{
    /// <summary>
    /// Interface for invoker cache service.
    /// </summary>
    public interface IInvokerCacheService : ICache<string, EntityInvoker>
    {
        /// <summary>
        /// Get detector invoker.
        /// </summary>
        /// <typeparam name="TResource">Resource type.</typeparam>
        /// <param name="entityId">Detector id.</param>
        /// <param name="context">Runtime context.</param>
        /// <returns>Entity invoker.</returns>
        EntityInvoker GetEntityInvoker<TResource>(string entityId, RuntimeContext<TResource> context)
            where TResource : IResource;

        /// <summary>
        /// Get detector invoker list.
        /// </summary>
        /// <typeparam name="TResource">Resource type.</typeparam>
        /// <param name="context">Runtime context.</param>
        /// <returns>Entity invoker list.</returns>
        IEnumerable<EntityInvoker> GetEntityInvokerList<TResource>(RuntimeContext<TResource> context)
            where TResource : IResource;

        /// <summary>
        /// Get system invoker.
        /// </summary>
        /// <param name="invokerId">Invoker id.</param>
        /// <returns>Entity invoker.</returns>
        EntityInvoker GetSystemInvoker(string invokerId);

        /// <summary>
        /// Get system invoker list.
        /// </summary>
        /// <typeparam name="TResource">Resource type.</typeparam>
        /// <param name="context">Runtime context.</param>
        /// <returns>Entity invoker list.</returns>
        IEnumerable<EntityInvoker> GetSystemInvokerList<TResource>(RuntimeContext<TResource> context)
            where TResource : IResource;
    }
}
