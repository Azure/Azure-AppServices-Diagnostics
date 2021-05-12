using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public class K8SELogAnalyticsDataProvider : LogAnalyticsDataProvider
    {
        private ILogAnalyticsClient _K8SELogAnalyticsClient;

        public K8SELogAnalyticsDataProvider(OperationDataCache cache, K8SELogAnalyticsDataProviderConfiguration configuration, string RequestId) : base(cache, configuration)
        {
            _K8SELogAnalyticsClient = LogAnalyticsClientFactory.GetLogAnalyticsClient(configuration, RequestId);

            Metadata = new DataProviderMetadata
            {
                ProviderName = "K8SELogAnalytics"
            };
        }

        public override Task<DataTable> ExecuteQueryAsync(string query)
        {
            AddQueryInformationToMetadata(query);
            return  _K8SELogAnalyticsClient.ExecuteQueryAsync(query);
        }
        private void AddQueryInformationToMetadata(string query)
        {
            var logAnalyticsQuery = _K8SELogAnalyticsClient.GetLogAnalyticsQuery(query);
            bool queryExists = false;

            queryExists = Metadata.PropertyBag.Any(x => x.Key == "Query" &&
                                                        x.Value.GetType() == typeof(LogAnalyticsQuery) &&
                                                        x.Value.CastTo<LogAnalyticsQuery>().Text.Equals(query, StringComparison.OrdinalIgnoreCase));
            if (!queryExists)
            {
                Metadata.PropertyBag.Add(new KeyValuePair<string, object>("Query", logAnalyticsQuery));
            }
        }
    }
}
