using Diagnostics.DataProviders.Interfaces;

namespace Diagnostics.DataProviders
{
    public class DataProviders
    {
        private OperationDataCache _cache = new OperationDataCache();

        public IKustoDataProvider Kusto;
        public ISupportObserverDataProvider Observer;
        public IGeoMasterDataProvider GeoMaster;
        public IAppInsightsDataProvider AppInsights;

        public DataProviders(DataProviderContext context)
        {
            Kusto = new DataProviderLogDecorator(context, new KustoDataProvider(_cache, context.Configuration.KustoConfiguration));
            Observer = new DataProviderLogDecorator(context, SupportObserverDataProviderFactory.GetDataProvider(_cache, context.Configuration));
            GeoMaster = new DataProviderLogDecorator(context, new GeoMasterDataProvider(_cache, context.Configuration.GeoMasterConfiguration));
            AppInsights = new DataProviderLogDecorator(context, new AppInsightsDataProvider(_cache, context.Configuration.AppInsightsConfiguration));
        }
    }
}
