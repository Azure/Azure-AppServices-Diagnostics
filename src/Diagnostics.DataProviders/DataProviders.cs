using System;
using Diagnostics.DataProviders.DataProviderConfigurations;
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
        public Func<GenericMdmDataProviderConfiguration, IMdmDataProvider> MdmGeneric;

        public DataProviders(DataProviderContext context)
        {
            Kusto = new KustoLogDecorator(context, new KustoDataProvider(_cache, context.Configuration.KustoConfiguration, context.RequestId, context.KustoHeartBeatService));
            Observer = new ObserverLogDecorator(context, SupportObserverDataProviderFactory.GetDataProvider(_cache, context.Configuration, context));
            GeoMaster = new GeoMasterLogDecorator(context, new GeoMasterDataProvider(_cache, context));
            AppInsights = new AppInsightsLogDecorator(context, new AppInsightsDataProvider(_cache, context.Configuration.AppInsightsConfiguration));
            ChangeAnalysis = new ChangeAnalysisLogDecorator(context, new ChangeAnalysisDataProvider(_cache, context.Configuration.ChangeAnalysisDataProviderConfiguration, context.RequestId, context.clientObjectId, context.clientPrincipalName, Kusto, context.receivedHeaders));
            Asc = new AscLogDecorator(context, new AscDataProvider(_cache, context.Configuration.AscDataProviderConfiguration, context.RequestId, context));
            Mdm = (MdmDataSource ds) =>
            {
                switch (ds)
                {
                    case MdmDataSource.Antares:
                        return new MdmLogDecorator(context, new MdmDataProvider(_cache, context.Configuration.AntaresMdmConfiguration, context.RequestId));
                    case MdmDataSource.Networking:
                        return new MdmLogDecorator(context, new MdmDataProvider(_cache, context.Configuration.NetworkingMdmConfiguration, context.RequestId));
                    default:
                        throw new NotSupportedException($"{ds} is not supported.");
                }
            };
            MdmGeneric = (GenericMdmDataProviderConfiguration config) => new MdmLogDecorator(context, new MdmDataProvider(_cache, new GenericMdmDataProviderConfigurationWrapper(config), context.RequestId, context.receivedHeaders));
        }
    }
}
