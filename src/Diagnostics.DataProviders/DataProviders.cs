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

        public KustoDataProvider Kusto;
        public ISupportObserverDataProvider Observer;
        public GeoMasterDataProvider GeoMaster;

        public DataProviders(DataSourcesConfiguration configuration)
        {
            Kusto = new KustoDataProvider(_cache, configuration.KustoConfiguration);
            Observer = SupportObserverDataProviderFactory.GetDataProvider(_cache, configuration);
            GeoMaster = new GeoMasterDataProvider(_cache, configuration.GeoMasterConfiguration);
        }
    }
}
