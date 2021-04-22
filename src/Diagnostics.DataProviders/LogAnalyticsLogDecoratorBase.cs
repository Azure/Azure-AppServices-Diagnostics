using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders
{
    abstract internal class LogAnalyticsLogDecorator : LogDecoratorBase, ILogAnalyticsDataProvider
    {
        public ILogAnalyticsDataProvider DataProvider;

        public LogAnalyticsLogDecorator(DataProviderContext context, ILogAnalyticsDataProvider dataProvider) : base(context, dataProvider.GetMetadata())
        {
            DataProvider = dataProvider;
        }

        public Task<DataTable> ExecuteQueryAsync(string query)
        {
            return MakeDependencyCall(DataProvider.ExecuteQueryAsync(query));
        }
    }
}
