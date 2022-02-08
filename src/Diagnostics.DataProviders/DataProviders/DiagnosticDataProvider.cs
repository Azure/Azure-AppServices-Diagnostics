using System;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Exceptions;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders
{
    public class DiagnosticDataProvider : IDiagnosticDataProvider, IHealthCheck
    {
        private OperationDataCache _cache;
        private IDataProviderConfiguration _configuration;
        private const string baseMessage = @"The underlying data source may not fully implement the health check interface or it may be missing data to complete a successful health check";

        public DiagnosticDataProvider(OperationDataCache cache, IDataProviderConfiguration configuration, bool hasCustomerConsent = false)
        {
            _cache = cache;
            _configuration = configuration;
            HasCustomerConsent = hasCustomerConsent;
        }

        public DataProviderMetadata Metadata { get; set; }

        private bool HasCustomerConsent;

        public IDataProviderConfiguration DataProviderConfiguration => _configuration;

        public bool IsEnabled => true;

        protected Task<T> GetOrAddFromCache<T>(string key, Func<string, CacheMember> addFunction)
        {
            return Convert.ChangeType(_cache.GetOrAdd(key, addFunction), typeof(Task<T>)) as Task<T>;
        }

        public virtual Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(new HealthCheckResult(HealthStatus.Unknown, this.GetType().Name, baseMessage));
        }

        public void EnforceCustomerConsent()
        {
            if (!HasCustomerConsent)
            {
                throw new CustomerConsentException(Metadata != null ? Metadata.ProviderName : null);
            }
        }
    }
}
