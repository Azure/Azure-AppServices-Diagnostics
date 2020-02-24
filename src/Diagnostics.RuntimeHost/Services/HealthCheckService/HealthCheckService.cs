using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.DataProviders;
using System.Collections.Generic;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Microsoft.IdentityModel.Tokens;
using System.Linq;

namespace Diagnostics.RuntimeHost.Services
{
    public interface IHealthCheckService : IDisposable
    {
        Task RunHealthCheck();
        Task<IEnumerable<HealthCheckResult>> RunDependencyCheck(DataProviders.DataProviders dataProviders);
    }

    public class HealthCheckService : IHealthCheckService
    {
        private string OutboundConnectivityCheckUrl;
        private HttpClient _httpClient;
        private readonly ISourceWatcher _sourceWatcher;
        IConfiguration _configuration;
        IDataSourcesConfigurationService _dataSourcesConfigurationService;
        bool IsOutboundConnectivityCheckEnabled = false;

        public HealthCheckService(IConfiguration Configuration, ISourceWatcherService sourceWatcherService, IDataSourcesConfigurationService dataProviderConfigurationService)
        {
            _configuration = Configuration;
            _sourceWatcher = sourceWatcherService.Watcher;
            _dataSourcesConfigurationService = dataProviderConfigurationService;
            IsOutboundConnectivityCheckEnabled = Convert.ToBoolean(_configuration["HealthCheckSettings:IsOutboundConnectivityCheckEnabled"]);
            if (IsOutboundConnectivityCheckEnabled)
            {
                OutboundConnectivityCheckUrl = _configuration["HealthCheckSettings:OutboundConnectivityCheckUrl"];
                if (OutboundConnectivityCheckUrl != null && OutboundConnectivityCheckUrl.Length > 0)
                {
                    InitializeHttpClient();
                }
                else
                {
                    throw new Exception("Invalid configuration for parameter - HealthCheckSettings:OutboundConnectivityCheckUrl");
                }
            }
        }

        private Task<HttpResponseMessage> Get(HttpRequestMessage request)
        {
            return _httpClient.SendAsync(request);
        }

        public async Task<bool> HealthCheckPing()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, OutboundConnectivityCheckUrl);
            var response = await Get(request);
            return response.IsSuccessStatusCode;
        }

        public async Task RunHealthCheck()
        {
            if (IsOutboundConnectivityCheckEnabled)
            await RetryHelper.RetryAsync(HealthCheckPing, "Healthping", "", 3, 100);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "<Pending>")]
        public async Task<IEnumerable<HealthCheckResult>> RunDependencyCheck(DataProviders.DataProviders dataProviders)
        {
            var healthCheckResultsTasks = new Dictionary<string, Task<HealthCheckResult>>();
            var dataProviderFields = dataProviders.GetType().GetFields();

            foreach (var dataProviderField in dataProviderFields)
            {
                if (dataProviderField.FieldType.IsInterface)
                {
                    DiagnosticDataProvider dp = ((LogDecoratorBase)dataProviderField.GetValue(dataProviders)).DataProvider;
                    if (dp != null && ((dp.DataProviderConfiguration?.Enabled).HasValue && dp.DataProviderConfiguration.Enabled))
                        healthCheckResultsTasks.Add(dp.GetType().Name, dp.CheckHealthAsync());
                }
            }

            healthCheckResultsTasks.Add(_sourceWatcher.GetType().Name, _sourceWatcher.CheckHealthAsync());
            healthCheckResultsTasks.Add("Mdm", ((LogDecoratorBase)dataProviders.MdmGeneric(_dataSourcesConfigurationService.Config.AntaresMdmConfiguration)).DataProvider.CheckHealthAsync());

            return await Task.Run<IEnumerable<HealthCheckResult>>(async () =>
            {
                List<HealthCheckResult> healthCheckResults = new List<HealthCheckResult>();
                foreach (var kv in healthCheckResultsTasks)
                {
                    Exception healthCheckException = null;
                    try
                    {
                        var result = await kv.Value;
                        healthCheckResults.Add(result);
                    }
                    catch (Exception ex)
                    {
                        healthCheckException = ex;
                        if (ex != null)
                        {
                            healthCheckResults.Add(new HealthCheckResult(HealthStatus.Unhealthy, kv.Key, ex: ex));
                        }
                    }
                }

                return healthCheckResults.OrderBy(x => x.Status);
            });
        }

        public void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }
        }

        private void InitializeHttpClient()
        {
            _httpClient = new HttpClient
            {
                MaxResponseContentBufferSize = Int32.MaxValue,
                Timeout = TimeSpan.FromSeconds(3)
            };
        }
    }
}
