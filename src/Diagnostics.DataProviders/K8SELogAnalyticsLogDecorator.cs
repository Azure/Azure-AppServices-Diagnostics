using System.Data;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders
{
    internal class K8SELogAnalyticsLogDecorator : LogDecoratorBase, ILogAnalyticsDataProvider
    {
        public ILogAnalyticsDataProvider DataProvider;

        public K8SELogAnalyticsLogDecorator(DataProviderContext context, ILogAnalyticsDataProvider dataProvider) : base(context, dataProvider.GetMetadata())
        {
            DataProvider = dataProvider;
        }

        public Task<DataTable> ExecuteQueryAsync(string query)
        {
            return MakeDependencyCall(DataProvider.ExecuteQueryAsync(query));
        }
    }
}
