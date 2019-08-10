using System;
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

        public DataProviders(DataProviderContext context)
        {
            Kusto = new DataProviderLogDecorator(context, new KustoDataProvider(_cache, context.Configuration.KustoConfiguration, context.RequestId, context.KustoHeartBeatService));
            Observer = new DataProviderLogDecorator(context, SupportObserverDataProviderFactory.GetDataProvider(_cache, context.Configuration, context));
            GeoMaster = new DataProviderLogDecorator(context, new GeoMasterDataProvider(_cache, context));
            AppInsights = new DataProviderLogDecorator(context, new AppInsightsDataProvider(_cache, context.Configuration.AppInsightsConfiguration));
            ChangeAnalysis = new DataProviderLogDecorator(context, new ChangeAnalysisDataProvider(_cache, context.Configuration.ChangeAnalysisDataProviderConfiguration, context.RequestId, context.clientObjectId, context.clientPrincipalName, Kusto));
            Asc = new DataProviderLogDecorator(context, new AscDataProvider(_cache, context.Configuration.AscDataProviderConfiguration, context.RequestId));
            Mdm = (MdmDataSource ds) =>
            {
                switch (ds)
                {
                    case MdmDataSource.Antares:
                        return new DataProviderLogDecorator(context, new MdmDataProvider(_cache, context.Configuration.AntaresMdmConfiguration, context.RequestId));             
                    default:
                        throw new NotSupportedException($"{ds} is not supported.");
                }
            };
        }
    }
}
