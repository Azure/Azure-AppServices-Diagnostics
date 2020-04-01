using System;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders;
using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services.CacheService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    public abstract class SourceWatcherBase : ISourceWatcher
    {
        protected IHostingEnvironment _env;
        protected IConfiguration _config;
        protected IInvokerCacheService _invokerCache;
        protected IGistCacheService _gistCache;
        protected IKustoMappingsCacheService _kustoMappingsCache;
        protected string _eventSource;

        protected abstract string SourceName { get; }

        protected abstract Task FirstTimeCompletionTask { get; }

        public abstract void Start();

        public Task WaitForFirstCompletion() => FirstTimeCompletionTask;

        public abstract Task CreateOrUpdatePackage(Package pkg);

        public abstract Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default(CancellationToken));

        protected SourceWatcherBase(IHostingEnvironment env, IConfiguration configuration, IInvokerCacheService invokerCache, IGistCacheService gistCache, IKustoMappingsCacheService kustoMappingsCache, string eventSource)
        {
            _env = env;
            _config = configuration;
            _invokerCache = invokerCache;
            _gistCache = gistCache;
            _kustoMappingsCache = kustoMappingsCache;
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
