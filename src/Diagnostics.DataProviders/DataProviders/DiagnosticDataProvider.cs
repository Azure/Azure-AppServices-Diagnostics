using System;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders
{
    public class DiagnosticDataProvider : IDiagnosticDataProvider, IHealthCheck
    {
        private OperationDataCache _cache;
        private IDataProviderConfiguration _configuration;
        private const string baseMessage = @"The underlying data source may not fully implement the health check interface or it may be missing data to complete a successful health check";

        public DiagnosticDataProvider(OperationDataCache cache, IDataProviderConfiguration configuration)
        {
            _cache = cache;
            _configuration = configuration;
        }

        public DataProviderMetadata Metadata { get; set; }

        private IDataProviderConfiguration DataProviderConfiguration => _configuration;

        public bool IsEnabled => _configuration?.Enabled == true;

        protected Task<T> GetOrAddFromCache<T>(string key, Func<string, CacheMember> addFunction)
        {
            return Convert.ChangeType(_cache.GetOrAdd(key, addFunction), typeof(Task<T>)) as Task<T>;
        }

        public virtual Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(new HealthCheckResult(HealthStatus.Unknown, this.GetType().Name, baseMessage));
        }
    }
}
