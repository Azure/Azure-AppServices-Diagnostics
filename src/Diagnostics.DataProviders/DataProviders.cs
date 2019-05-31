using Diagnostics.DataProviders.Interfaces;
using System;

namespace Diagnostics.DataProviders
{
    public class DataProviders
    {
        private OperationDataCache _cache = new OperationDataCache();

        public IKustoDataProvider Kusto { get; }
        public ISupportObserverDataProvider Observer { get; }
        public IGeoMasterDataProvider GeoMaster { get; }
        public IAppInsightsDataProvider AppInsights { get; }
        public IChangeAnalysisDataProvider ChangeAnalysis { get; }
        public IAscDataProvider Asc { get; }
        public Func<MdmDataSource, IMdmDataProvider> Mdm { get; }

        public DataProviders(DataProviderContext context)
        {
            Kusto = new DataProviderLogDecorator(context, new KustoDataProvider(_cache, context.Configuration.KustoConfiguration, context.RequestId, context.KustoHeartBeatService));
            Observer = new DataProviderLogDecorator(context, SupportObserverDataProviderFactory.GetDataProvider(_cache, context.Configuration, context));
            GeoMaster = new DataProviderLogDecorator(context, new GeoMasterDataProvider(_cache, context.Configuration.GeoMasterConfiguration));
            AppInsights = new DataProviderLogDecorator(context, new AppInsightsDataProvider(_cache, context.Configuration.AppInsightsConfiguration));
            ChangeAnalysis = new DataProviderLogDecorator(context, new ChangeAnalysisDataProvider(_cache, context.Configuration.ChangeAnalysisDataProviderConfiguration, context.RequestId, context.clientObjectId, context.clientPrincipalName, Kusto));
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
