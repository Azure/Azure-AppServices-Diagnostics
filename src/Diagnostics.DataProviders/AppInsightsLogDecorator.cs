using System.Data;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;

namespace Diagnostics.DataProviders
{
    internal class AppInsightsLogDecorator : LogDecoratorBase, IAppInsightsDataProvider
    {
        public IAppInsightsDataProvider DataProvider;

        public AppInsightsLogDecorator(DataProviderContext context, IAppInsightsDataProvider dataProvider) : base(context, dataProvider.GetMetadata())
        {
            DataProvider = dataProvider;
        }

        public Task<DataTable> ExecuteAppInsightsQuery(string query)
        {
            return MakeDependencyCall(DataProvider.ExecuteAppInsightsQuery(query));
        }

        public Task<bool> SetAppInsightsKey(string appId, string apiKey)
        {
            return MakeDependencyCall(DataProvider.SetAppInsightsKey(appId, apiKey));
        }
    }
}
