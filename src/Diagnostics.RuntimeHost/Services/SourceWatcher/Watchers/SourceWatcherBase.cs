using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Models;
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
        protected string _eventSource;

        protected abstract Task FirstTimeCompletionTask { get; }

        public abstract void Start();

        public Task WaitForFirstCompletion() => FirstTimeCompletionTask;

        public abstract Task<Tuple<bool, Exception>> CreateOrUpdateDetector(DetectorPackage pkg);

        public SourceWatcherBase(IHostingEnvironment env, IConfiguration configuration, IInvokerCacheService invokerCache, string eventSource)
        {
            _env = env;
            _config = configuration;
            _invokerCache = invokerCache;
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
            string exceptionType = ex != null ? ex.GetType().ToString() : string.Empty;
            string exceptionDetails = ex != null ? ex.ToString() : string.Empty;

            DiagnosticsETWProvider.Instance.LogSourceWatcherException(_eventSource, message, exceptionType, exceptionDetails);
        }
    }
}
