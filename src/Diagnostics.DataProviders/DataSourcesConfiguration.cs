using Diagnostics.DataProviders.DataProviderConfigurations;

namespace Diagnostics.DataProviders
{
    public class DataSourcesConfiguration
    {
        public KustoDataProviderConfiguration KustoConfiguration { get; set; }

        public SupportObserverDataProviderConfiguration SupportObserverConfiguration { get; set; }

        public GeoMasterDataProviderConfiguration GeoMasterConfiguration  { get; set; }

        public AppInsightsDataProviderConfiguration AppInsightsConfiguration { get; set; }

        public MdmDataProviderConfiguration MdmConfiguration { get; set; }

        public ChangeAnalysisDataProviderConfiguration ChangeAnalysisDataProviderConfiguration { get; set; }
    }
}
