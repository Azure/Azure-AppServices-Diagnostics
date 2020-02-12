// <copyright file="GithubGistWorker.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher.Workers
{
    /// <summary>
    /// Gist worker.
    /// </summary>
    public class GithubGistWorker : GithubScriptWorkerBase
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        public override string Name => "GistWorker";

        /// <summary>
        /// Gets the gist cache.
        /// </summary>
        public IGistCacheService GistCache { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GithubGistWorker"/> class.
        /// </summary>
        /// <param name="gistCache">Gist cache service.</param>
        public GithubGistWorker(IGistCacheService gistCache, bool loadOnlyPublicDetectors, IGithubClient githubClient) : base(loadOnlyPublicDetectors, githubClient)
        {
            GistCache = gistCache;
        }

        protected override ICache<string, EntityInvoker> GetCacheService()
        {
            return GistCache;
        }

        protected override EntityType GetEntityType()
        {
            return EntityType.Gist;
        }
    }
}
