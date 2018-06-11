using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public class DataProviders
    {
        private OperationDataCache _cache = new OperationDataCache();

        public IKustoDataProvider Kusto;
        public ISupportObserverDataProvider Observer;
        public IGeoMasterDataProvider GeoMaster;

        public DataProviders(DataSourcesConfiguration configuration)
        {
            Kusto = new DataProviderLogDecorator(new KustoDataProvider(_cache, configuration.KustoConfiguration));
            Observer = new DataProviderLogDecorator(SupportObserverDataProviderFactory.GetDataProvider(_cache, configuration));
            GeoMaster = new DataProviderLogDecorator(new GeoMasterDataProvider(_cache, configuration.GeoMasterConfiguration));
        }
    }
}
