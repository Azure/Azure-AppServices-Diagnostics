using Diagnostics.DataProviders.DataProviderConfigurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public class DataSourcesConfiguration
    {
        public KustoDataProviderConfiguration KustoConfiguration { get; set; }
        public SupportObserverDataProviderConfiguration SupportObserverConfiguration { get; set; }
        public GeoMasterDataProviderConfiguration GeoMasterConfiguration  { get; set; }
        public AppInsightsDataProviderConfiguration AppInsightsConfiguration { get; set; }
    }
}
