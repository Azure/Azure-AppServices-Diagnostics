using System;
using System.Collections.Generic;
using System.Linq;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;

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

        private readonly List<LogDecoratorBase> _dataProviderList = new List<LogDecoratorBase>();

        public DataProviders(DataProviderContext context)
        {

            Kusto = GetOrAddDataProvider(new KustoLogDecorator(context, new KustoDataProvider(_cache,
                     context.Configuration.KustoConfiguration,
                     context.RequestId,
                     context.KustoHeartBeatService)));

            Observer = GetOrAddDataProvider(new ObserverLogDecorator(context, SupportObserverDataProviderFactory.GetDataProvider(_cache, context.Configuration, context)));

            GeoMaster = GetOrAddDataProvider(new GeoMasterLogDecorator(context, new GeoMasterDataProvider(_cache, context)));

            AppInsights = GetOrAddDataProvider(new AppInsightsLogDecorator(context, new AppInsightsDataProvider(_cache, context.Configuration.AppInsightsConfiguration)));

            ChangeAnalysis = GetOrAddDataProvider(new ChangeAnalysisLogDecorator(context, new ChangeAnalysisDataProvider(_cache, context.Configuration.ChangeAnalysisDataProviderConfiguration, context.RequestId, context.clientObjectId, context.clientPrincipalName, Kusto, context.receivedHeaders)));

            Asc = GetOrAddDataProvider(new AscLogDecorator(context, new AscDataProvider(_cache, context.Configuration.AscDataProviderConfiguration, context.RequestId, context)));

            Mdm = (MdmDataSource ds) =>
            {
                switch (ds)
                {
                    case MdmDataSource.Antares:
                        return GetOrAddDataProvider(new MdmLogDecorator(context, new MdmDataProvider(_cache, context.Configuration.AntaresMdmConfiguration, context.RequestId)));
                    case MdmDataSource.Networking:
                        return GetOrAddDataProvider(new MdmLogDecorator(context, new MdmDataProvider(_cache, context.Configuration.NetworkingMdmConfiguration, context.RequestId)));
                    default:
                        throw new NotSupportedException($"{ds} is not supported.");
                }
            };

            MdmGeneric = (GenericMdmDataProviderConfiguration config) =>
            {
                return GetOrAddDataProvider(new MdmLogDecorator(context, new MdmDataProvider(_cache, new GenericMdmDataProviderConfigurationWrapper(config), context.RequestId, context.receivedHeaders)));
            };
        }

        private T GetOrAddDataProvider<T>(T dataProvider) where T : LogDecoratorBase
        {
            _dataProviderList.Add(dataProvider);
            return dataProvider;
        }

        public List<DataProviderMetadata> GetMetadata()
        {
            return _dataProviderList.Where(d => d != null && d.GetMetadata() != null)
                .Select(d => d.GetMetadata()).ToList();
        }
    }
}
