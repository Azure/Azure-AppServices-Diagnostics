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

        public DataProviders(DataSourcesConfiguration configuration)
        {
            Kusto = new DataProviderLogDecorator(new KustoDataProvider(_cache, configuration.KustoConfiguration));
            Observer = new DataProviderLogDecorator(SupportObserverDataProviderFactory.GetDataProvider(_cache, configuration));
            GeoMaster = new DataProviderLogDecorator(new GeoMasterDataProvider(_cache, configuration.GeoMasterConfiguration));
            AppInsights = new DataProviderLogDecorator(new AppInsightsDataProvider(_cache, configuration.AppInsightsConfiguration));
        }
    }
}
