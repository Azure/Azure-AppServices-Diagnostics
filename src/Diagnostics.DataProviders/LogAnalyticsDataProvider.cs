using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public abstract class LogAnalyticsDataProvider : DiagnosticDataProvider, IDiagnosticDataProvider, ILogAnalyticsDataProvider
    {
        public class LogAnalyticsQuery
        {
            public string Text;
        }
        public LogAnalyticsDataProvider(OperationDataCache cache, LogAnalyticsDataProviderConfiguration configuration) : base(cache, configuration)
        {

        }
        public abstract Task<DataTable> ExecuteQueryAsync(string query);

        public DataProviderMetadata GetMetadata()
        {
            return Metadata;
        }
    }
}
