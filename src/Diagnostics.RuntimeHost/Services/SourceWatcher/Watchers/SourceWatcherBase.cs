using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.Scripts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    public abstract class SourceWatcherBase : ISourceWatcher
    {
        protected IHostingEnvironment _env;
        protected IConfiguration _config;
        protected IInvokerCacheService _invokerCache;
        protected IGistCacheService _gistCache;
        protected string _eventSource;

        protected abstract string SourceName { get; }

        protected abstract Task FirstTimeCompletionTask { get; }

        public abstract void Start();

        public Task WaitForFirstCompletion() => FirstTimeCompletionTask;

        public abstract Task CreateOrUpdatePackage(Package pkg);

        protected SourceWatcherBase(IHostingEnvironment env, IConfiguration configuration, IInvokerCacheService invokerCache, IGistCacheService gistCache, string eventSource)
        {
            _env = env;
            _config = configuration;
            _invokerCache = invokerCache;
            _gistCache = gistCache;
            _eventSource = eventSource;
        }

        protected void LogMessage(string message)
        {
            DiagnosticsETWProvider.Instance.LogSourceWatcherMessage(_eventSource, message);
        }

        protected void LogWarning(string message)
        {
            DiagnosticsETWProvider.Instance.LogSourceWatcherWarning(_eventSource, message);
        }

        protected void LogException(string message, Exception ex)
        {
            var exception = new SourceWatcherException(SourceName, message, ex);
            DiagnosticsETWProvider.Instance.LogSourceWatcherException(_eventSource, message, exception.GetType().ToString(), exception.ToString());
        }
    }
}
