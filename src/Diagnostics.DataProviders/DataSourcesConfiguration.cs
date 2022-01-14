using Diagnostics.DataProviders.DataProviderConfigurations;

namespace Diagnostics.DataProviders
{
    public class DataSourcesConfiguration
    {
        public KustoDataProviderConfiguration KustoConfiguration { get; set; }

        public SupportObserverDataProviderConfiguration SupportObserverConfiguration { get; set; }

        public GeoMasterDataProviderConfiguration GeoMasterConfiguration { get; set; }

        public AppInsightsDataProviderConfiguration AppInsightsConfiguration { get; set; }

        public ChangeAnalysisDataProviderConfiguration ChangeAnalysisDataProviderConfiguration { get; set; }

        public AscDataProviderConfiguration AscDataProviderConfiguration { get; set; }

        public K8SELogAnalyticsDataProviderConfiguration K8SELogAnalyticsConfiguration { get; set; }

        public AntaresMdmDataProviderConfiguration AntaresMdmConfiguration { get; set; }

        public ContainerAppsMdmDataProviderConfiguration ContainerAppsMdmConfiguration { get; set; }

        public NetworkingMdmDataProviderConfiguration NetworkingMdmConfiguration { get; set; }

        public SearchServiceProviderConfiguration SearchServiceProviderConfiguration { get; set; }
    }
}
