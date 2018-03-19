﻿using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.Scripts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    public class SourceWatcherService : ISourceWatcherService
    {
        private ISourceWatcher _watcher;

        public ISourceWatcher Watcher => _watcher;

        public SourceWatcherService(IHostingEnvironment env, IConfiguration configuration, ICache<string, EntityInvoker> invokerCacheService)
        {
            SourceWatcherType watcherType;

            if (env.IsProduction())
            {
                string watcherTypeRegistryValue = Registry.GetValue(RegistryConstants.SourceWatcherRegistryPath, RegistryConstants.WatcherTypeKey, 0).ToString();
                if(!Enum.TryParse<SourceWatcherType>(watcherTypeRegistryValue, out watcherType))
                {
                    throw new NotSupportedException($"Source Watcher Type : {watcherTypeRegistryValue} not supported.");
                }
            }
            else
            {
                watcherType = Enum.Parse<SourceWatcherType>(configuration[$"SourceWatcher:{RegistryConstants.WatcherTypeKey}"]);
            }

            switch (watcherType)
            {
                case SourceWatcherType.LocalFileSystem:
                    _watcher = new LocalFileSystemWatcher(env, configuration, invokerCacheService);
                    break;
                case SourceWatcherType.Github:
                    IGithubClient githubClient = new GithubClient(env, configuration);
                    _watcher = new GitHubWatcher(env, configuration, invokerCacheService, githubClient);
                    break;
                default:
                    throw new NotSupportedException("Source Watcher Type not supported");
            }
        }
    }
}
