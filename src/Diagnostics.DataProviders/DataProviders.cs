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

        public DataProviders(DataSourcesConfiguration configuration)
        {
            Kusto = new KustoDataProvider(_cache, configuration.KustoConfiguration);
        }
    }
}
