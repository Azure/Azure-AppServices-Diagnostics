﻿using System;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;
using Diagnostics.RuntimeHost.Services.SourceWatcher.Watchers;
using Diagnostics.RuntimeHost.Services.StorageService;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    public class SourceWatcherService : ISourceWatcherService
    {
        private ISourceWatcher _watcher;

        public ISourceWatcher Watcher => _watcher;

        public ISourceWatcher KustoMappingWatcher;


        public SourceWatcherService(IHostingEnvironment env, IConfiguration configuration, IInvokerCacheService invokerCacheService, IGistCacheService gistCacheService, 
            IKustoMappingsCacheService kustoMappingsCacheService, IStorageService storageService, IGithubClient githubClient, IDiagEntityTableCacheService tableCacheService)
        {
            var sourceWatcherType = Enum.Parse<SourceWatcherType>(configuration[$"SourceWatcher:{RegistryConstants.WatcherTypeKey}"]);
            switch (sourceWatcherType)
            {
                case SourceWatcherType.LocalFileSystem:
                    _watcher = new LocalFileSystemWatcher(env, configuration, invokerCacheService, gistCacheService);
                    break;
                case SourceWatcherType.Github:
                    _watcher = new GitHubWatcher(env, configuration, invokerCacheService, gistCacheService, kustoMappingsCacheService, githubClient);
                    break;
                case SourceWatcherType.AzureStorage:
                    _watcher = new StorageWatcher(env, configuration, storageService, invokerCacheService, gistCacheService, kustoMappingsCacheService, githubClient, tableCacheService);
                    break;
                default:
                    throw new NotSupportedException("Source Watcher Type not supported");
            }

            _watcher.Start();
        }
    }
}
