using System.Data;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders
{
    internal class K8SELogAnalyticsLogDecorator : LogDecoratorBase, IK8SELogAnalyticsDataProvider
    {
        public IK8SELogAnalyticsDataProvider DataProvider;

        public K8SELogAnalyticsLogDecorator(DataProviderContext context, IK8SELogAnalyticsDataProvider dataProvider) : base(context, dataProvider.GetMetadata())
        {
            DataProvider = dataProvider;
        }

        public Task<DataTable> ExecuteQueryAsync(string query)
        {
            return MakeDependencyCall(DataProvider.ExecuteQueryAsync(query));
        }
    }
}
