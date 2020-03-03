using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Diagnostics.RuntimeHost.Utilities
{
    public interface IRuntimeLoggerProvider : ILoggerProvider
    {
        void WriteLog(RuntimeLogEntry info);
        IEnumerable<RuntimeLogEntry> GetAndClear(string category);
    }

    // Documents used to develop this class:
    //    https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-3.1
    //    https://www.codeproject.com/Articles/1556475/How-to-write-a-custom-logging-provider-in-Asp-Net
    //    https://github.com/tbebekis/AspNetCore-CustomLoggingProvider/blob/master/WebApp/Controllers/HomeController.cs
    internal class RuntimeLogger : ILogger
    {
        public RuntimeLogProvider Provider { get; }
        public string Category { get; }

        public RuntimeLogger(RuntimeLogProvider provider, string category)
        {
            this.Provider = provider;
            this.Category = category;
        }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return this.Provider.ScopeProvider.Push(state);
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return this.Provider.IsEnabled(logLevel);
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var isEnabled = (this as ILogger).IsEnabled(logLevel);

            if (!isEnabled)
            {
                return;
            }

            var entry = new RuntimeLogEntry
            {
                Category = this.Category,
                Level = logLevel,
                Message = exception?.Message ?? state.ToString(),
                Exception = exception,
                EventId = eventId.Id,
                State = state,
            };

            if (state is string)
            {
                entry.StateText = state.ToString();
            }
            else if (state is IEnumerable<KeyValuePair<string, object>> properties)
            {
                entry.StateProperties = new Dictionary<string, object>();
                foreach(var item in properties)
                {
                    entry.StateProperties[item.Key] = item.Value;
                }
            }

            if (this.Provider.ScopeProvider != null)
            {
                this.Provider.ScopeProvider.ForEachScope((obj, loggingProps) =>
                {
                    if (entry.Scopes == null)
                    {
                        entry.Scopes = new List<RuntimeLogScope>();
                    }

                    var scope = new RuntimeLogScope();
                    entry.Scopes.Add(scope);

                    if (obj is string)
                    {
                        scope.Text = obj.ToString();
                    }
                    else if (obj is IEnumerable<KeyValuePair<string, object>> properties)
                    {
                        if (scope.Properties == null)
                        {
                            scope.Properties = new Dictionary<string, object>();
                        }

                        foreach (var item in properties)
                        {
                            scope.Properties[item.Key] = item.Value;
                        }
                    }
                }, state);
            }

            this.Provider.WriteLog(entry);
        }
    }

    [Microsoft.Extensions.Logging.ProviderAlias("Runtime")]
    public class RuntimeLogProvider : IDisposable, IRuntimeLoggerProvider, ISupportExternalScope
    {
        private ConcurrentDictionary<string, RuntimeLogger> _loggers = new ConcurrentDictionary<string, RuntimeLogger>();
        private IExternalScopeProvider _scopeProvider;
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
        private IDisposable _settingsChangeToken;
        private bool _disposed = false;
        private static ConcurrentDictionary<string, List<RuntimeLogEntry>> _runtimeLogs = new ConcurrentDictionary<string, List<RuntimeLogEntry>>();

        public RuntimeLogProvider()
        {
        }

        ~RuntimeLogProvider()
        {
            if (!_disposed)
            {
                Dispose(false);
            }
        }

        internal IExternalScopeProvider ScopeProvider
        {
            get
            {
                if (_scopeProvider == null)
                {
                    _scopeProvider = new LoggerExternalScopeProvider();
                }

                return _scopeProvider;
            }
        }

        public void WriteLog(RuntimeLogEntry info)
        {
            if (info == null || info.Message == null)
            {
                return;
            }

            var reqid = info.Category;
            _runtimeLogs.AddOrUpdate(reqid,
                id => new List<RuntimeLogEntry> { info }, // Adding
                (id, logs) => // Updating
                {
                    logs.Add(info);
                    return logs;
                });

            // TODO: emit ETW event
        }

        public IEnumerable<RuntimeLogEntry> GetAndClear(string category)
        {
            var reqid = category;
            List<RuntimeLogEntry> logs = null;

            _runtimeLogs.TryRemove(reqid, out logs);

            return logs;
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public bool IsEnabled(LogLevel level)
        {
            bool result = level != LogLevel.None
                && this.LogLevel != LogLevel.None
                && (int)level >= (int)this.LogLevel;

            return result;
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return null;
            }
            return _loggers.GetOrAdd(categoryName, (category) => new RuntimeLogger(this, category));
        }

        void IDisposable.Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    Dispose(true);
                }
                catch
                {
                }

                _disposed = true;
                GC.SuppressFinalize(this);  // instructs GC not bother to call the destructor   
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_settingsChangeToken != null)
            {
                _settingsChangeToken.Dispose();
                _settingsChangeToken = null;
            }
        }
    }

    public static class RuntimeLoggerExtensions
    {
        public static ILoggingBuilder AddRuntimeLogger(this ILoggingBuilder builder)
        {
            if (builder != null)
            {
                builder.Services.Add(ServiceDescriptor.Singleton<IRuntimeLoggerProvider, RuntimeLogProvider>());
            }

            return builder;
        }
    }
}
