// <copyright file="IGistCacheService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System.Collections.Generic;
using System.Collections.Immutable;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.Scripts;

namespace Diagnostics.RuntimeHost.Services.CacheService
{
    /// <summary>
    /// Gist cache service interface.
    /// </summary>
    public interface IGistCacheService : ICache<string, EntityInvoker>
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
        /// Get all references.
        /// </summary>
        /// <typeparam name="TResource">The resource type.</typeparam>
        /// <param name="context">Runtime context.</param>
        /// <returns>All references.</returns>
        IImmutableDictionary<string, string> GetAllReferences<TResource>(RuntimeContext<TResource> context)
            where TResource : IResource;
    }
}
