using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.DataProviders;
using System.Collections.Generic;
using Diagnostics.RuntimeHost.Services.SourceWatcher;

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

        public async Task<IEnumerable<HealthCheckResult>> RunDependencyCheck(DataProviders.DataProviders dataProviders)
        {
            var healthCheckResultsTask = new List<Task<HealthCheckResult>>();
            var dataProviderFields = dataProviders.GetType().GetFields();

            foreach (var dataProviderField in dataProviderFields)
            {
                if (dataProviderField.FieldType.IsInterface)
                {
                    DiagnosticDataProvider dp = ((LogDecoratorBase)dataProviderField.GetValue(dataProviders)).DataProvider;
                    if (dp != null)
                        healthCheckResultsTask.Add(dp.CheckHealthAsync());
                }
            }

            healthCheckResultsTask.Add(_sourceWatcher.CheckHealthAsync());
            healthCheckResultsTask.Add(((DiagnosticDataProvider)dataProviders.MdmGeneric(_dataSourcesConfigurationService.Config.AntaresMdmConfiguration)).CheckHealthAsync());

            return await Task.WhenAll(healthCheckResultsTask);
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
