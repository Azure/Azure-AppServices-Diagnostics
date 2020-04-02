using System;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    public class SourceWatcherService : ISourceWatcherService
    {
        private ISourceWatcher _watcher;

        public ISourceWatcher Watcher => _watcher;

        public SourceWatcherService(IHostingEnvironment env, IConfiguration configuration, IInvokerCacheService invokerCacheService, IGistCacheService gistCacheService, IKustoMappingsCacheService kustoMappingsCacheService)
        {
            SourceWatcherType watcherType;

            
            watcherType = Enum.Parse<SourceWatcherType>(configuration[$"SourceWatcher:{RegistryConstants.WatcherTypeKey}"]);

            switch (watcherType)
            {
                case SourceWatcherType.LocalFileSystem:
                    _watcher = new LocalFileSystemWatcher(env, configuration, invokerCacheService, gistCacheService);
                    break;
                case SourceWatcherType.Github:
                    IGithubClient githubClient = new GithubClient(env, configuration);
                    _watcher = new GitHubWatcher(env, configuration, invokerCacheService, gistCacheService, kustoMappingsCacheService, githubClient);
                    break;
                default:
                    throw new NotSupportedException("Source Watcher Type not supported");
            }
        }
    }
}
