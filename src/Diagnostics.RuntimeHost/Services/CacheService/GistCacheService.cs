// <copyright file="GistCacheService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System.Collections.Generic;
using System.Collections.Immutable;
using Diagnostics.RuntimeHost.Models;

namespace Diagnostics.RuntimeHost.Services.CacheService
{
    /// <summary>
    /// Gist cache service.
    /// </summary>
    public class GistCacheService : InvokerCacheService, IGistCacheService
    {
        /// <summary>
        /// Get all references.
        /// </summary>
        /// <typeparam name="TResource">The resource type.</typeparam>
        /// <param name="context">Runtime context.</param>
        /// <returns>All references.</returns>
        IImmutableDictionary<string, string> IGistCacheService.GetAllReferences<TResource>(RuntimeContext<TResource> context)
        {
            var list = GetEntityInvokerList(context);
            var references = new Dictionary<string, string>();
            foreach (var invoker in list)
            {
                references.Add(invoker.EntryPointDefinitionAttribute.Id, invoker.EntityMetadata.ScriptText);
            }

            return references.ToImmutableDictionary();
        }
    }
}
