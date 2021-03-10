﻿using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public class K8SELogAnalyticsDataProvider : DiagnosticDataProvider, IDiagnosticDataProvider, IK8SELogAnalyticsDataProvider
    {
        private K8SELogAnalyticsDataProviderConfiguration _configuration;
        private IK8SELogAnalyticsClient _K8SELogAnalyticsClient;
        public K8SELogAnalyticsDataProvider(OperationDataCache cache, K8SELogAnalyticsDataProviderConfiguration configuration) : base(cache, configuration)
        {
            _configuration = configuration;
            _K8SELogAnalyticsClient = new K8SELogAnalyticsClient(_configuration);
            //Metadata = new DataProviderMetadata
            //{
            //    ProviderName = "AppInsights"
            //};
        }

        public Task<DataTable> ExecuteQueryAsync(string query)
        {
            return _K8SELogAnalyticsClient.ExecuteQueryAsync(query);
        }

        public DataProviderMetadata GetMetadata()
        {
            return Metadata;
        }
    }
}
