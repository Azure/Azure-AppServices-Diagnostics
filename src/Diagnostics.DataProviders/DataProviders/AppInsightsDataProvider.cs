using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.DataProviders.DataProviderConfigurations;
using System.Data;

namespace Diagnostics.DataProviders
{
    public class AppInsightsDataProvider : DiagnosticDataProvider, IDiagnosticDataProvider, IAppInsightsDataProvider
    {
        private readonly IAppInsightsClient _appInsightsClient;
        private AppInsightsDataProviderConfiguration _configuration;

        public AppInsightsDataProvider(OperationDataCache cache, AppInsightsDataProviderConfiguration configuration) : base(cache)
        {
            _configuration = configuration;
            _appInsightsClient = new AppInsightsClient(_configuration);
        }
        public Task<bool> SetAppInsightsKey(string appId, string apiKey)
        {
            _appInsightsClient.SetAppInsightsKey(appId, apiKey);
            return Task.FromResult(true);
        }

        public async Task<DataTable> ExecuteAppInsightsQuery(string query)
        {
            return await _appInsightsClient.ExecuteQueryAsync(query);
        }

        public DataProviderMetadata GetMetadata()
        {
            return null;
        }
    }
}
