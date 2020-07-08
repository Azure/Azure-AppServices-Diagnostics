using System;
using Diagnostics.RuntimeHost.Services.CacheService;
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


        public SourceWatcherService(IHostingEnvironment env, IConfiguration configuration, IInvokerCacheService invokerCacheService, IGistCacheService gistCacheService, IKustoMappingsCacheService kustoMappingsCacheService, IStorageService storageService)
        {
            var sourceWatcherType = Enum.Parse<SourceWatcherType>(configuration[$"SourceWatcher:{RegistryConstants.WatcherTypeKey}"]);
            IGithubClient githubClient = new GithubClient(env, configuration);
            switch (sourceWatcherType)
            {
                case SourceWatcherType.LocalFileSystem:
                    _watcher = new LocalFileSystemWatcher(env, configuration, invokerCacheService, gistCacheService);
                    break;
                case SourceWatcherType.Github:
                    _watcher = new GitHubWatcher(env, configuration, invokerCacheService, gistCacheService, kustoMappingsCacheService, githubClient);
                    break;
                case SourceWatcherType.AzureStorage:
                    _watcher = new StorageWatcher(env, configuration, storageService, invokerCacheService, gistCacheService);
                    KustoMappingWatcher = new GitHubWatcher(env, configuration, invokerCacheService, gistCacheService, kustoMappingsCacheService, githubClient);
                    break;
                default:
                    throw new NotSupportedException("Source Watcher Type not supported");
            }
        }
    }
}
