using System;
using System.Net.Http;
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
        public IChangeAnalysisDataProvider ChangeAnalysis;
        public IAscDataProvider Asc;
        public Func<MdmDataSource, IMdmDataProvider> Mdm;

        public DataProviders(DataProviderContext context, IHttpClientFactory httpClientFactory)
        {
            Kusto = new DataProviderLogDecorator(context, new KustoDataProvider(_cache, context.Configuration.KustoConfiguration, context.RequestId, httpClientFactory));
            Observer = new DataProviderLogDecorator(context, SupportObserverDataProviderFactory.GetDataProvider(_cache, context.Configuration, context));
            GeoMaster = new DataProviderLogDecorator(context, new GeoMasterDataProvider(_cache, context.Configuration.GeoMasterConfiguration));
            AppInsights = new DataProviderLogDecorator(context, new AppInsightsDataProvider(_cache, context.Configuration.AppInsightsConfiguration));
            ChangeAnalysis = new DataProviderLogDecorator(context, new ChangeAnalysisDataProvider(_cache, context.Configuration.ChangeAnalysisDataProviderConfiguration, context.Configuration.KustoConfiguration, context.RequestId, context.clientObjectId, context.clientPrincipalName, httpClientFactory));
            Asc = new DataProviderLogDecorator(context, new AscDataProvider(_cache, context.Configuration.AscDataProviderConfiguration, context.RequestId));
            Mdm = (MdmDataSource ds) =>
            {
                switch (ds)
                {
                    case MdmDataSource.Antares:
                        return new DataProviderLogDecorator(context, new MdmDataProvider(_cache, context.Configuration.AntaresMdmConfiguration, context.RequestId));
                    case MdmDataSource.Networking:
                        return new DataProviderLogDecorator(context, new MdmDataProvider(_cache, context.Configuration.NetworkingMdmConfiguration, context.RequestId));
                    default:
                        throw new NotSupportedException($"{ds} is not supported.");
                }
            };
        }
    }
}
