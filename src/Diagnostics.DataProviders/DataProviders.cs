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

        private readonly Dictionary<object, IMetadataProvider> _dataProvidersCache = new Dictionary<object, IMetadataProvider>();

        public DataProviders(DataProviderContext context)
        {

            Kusto = FromMemoryCache("Kusto", context) as IKustoDataProvider;
            Observer = FromMemoryCache("Observer", context) as ISupportObserverDataProvider;
            GeoMaster = FromMemoryCache("GeoMaster", context) as IGeoMasterDataProvider;
            AppInsights = FromMemoryCache("AppInsights", context) as IAppInsightsDataProvider;
            ChangeAnalysis = FromMemoryCache("ChangeAnalysis", context) as IChangeAnalysisDataProvider;
            Asc = FromMemoryCache("Asc", context) as IAscDataProvider;
            Mdm = (MdmDataSource ds) => FromMemoryCache(ds, context) as IMdmDataProvider;
            MdmGeneric = (GenericMdmDataProviderConfiguration config) => FromMemoryCache(config, context) as IMdmDataProvider;
        }

        private IMetadataProvider FromMemoryCache(object key, DataProviderContext context)
        {
            if (_dataProvidersCache.ContainsKey(key))
            {
                return _dataProvidersCache[key] as IMetadataProvider;
            }
            else
            {
                IMetadataProvider dataProviderObject = null;
                if (key is string keyString)
                {
                    switch (keyString)
                    {
                        case "Kusto":
                            dataProviderObject = new KustoLogDecorator(context, new KustoDataProvider(_cache, context.Configuration.KustoConfiguration, context.RequestId, context.KustoHeartBeatService));
                            break;
                        case "Observer":
                            dataProviderObject = new ObserverLogDecorator(context, SupportObserverDataProviderFactory.GetDataProvider(_cache, context.Configuration, context));
                            break;
                        case "GeoMaster":
                            dataProviderObject = new GeoMasterLogDecorator(context, new GeoMasterDataProvider(_cache, context));
                            break;
                        case "AppInsights":
                            dataProviderObject = new AppInsightsLogDecorator(context, new AppInsightsDataProvider(_cache, context.Configuration.AppInsightsConfiguration));
                            break;
                        case "ChangeAnalysis":
                            dataProviderObject = new ChangeAnalysisLogDecorator(context, new ChangeAnalysisDataProvider(_cache, context.Configuration.ChangeAnalysisDataProviderConfiguration, context.RequestId, context.clientObjectId, context.clientPrincipalName, Kusto, context.receivedHeaders));
                            break;
                        case "Asc":
                            dataProviderObject = new AscLogDecorator(context, new AscDataProvider(_cache, context.Configuration.AscDataProviderConfiguration, context.RequestId, context));
                            break;
                        default:
                            break;
                    }
                }
                else if (key is MdmDataSource ds)
                {
                    switch (ds)
                    {
                        case MdmDataSource.Antares:
                            dataProviderObject = new MdmLogDecorator(context, new MdmDataProvider(_cache, context.Configuration.AntaresMdmConfiguration, context.RequestId));
                            break;
                        case MdmDataSource.Networking:
                            dataProviderObject = new MdmLogDecorator(context, new MdmDataProvider(_cache, context.Configuration.NetworkingMdmConfiguration, context.RequestId));
                            break;
                        default:
                            throw new NotSupportedException($"{ds} is not supported.");
                    }
                }
                else if (key is GenericMdmDataProviderConfiguration config)
                {
                    dataProviderObject = new MdmLogDecorator(context, new MdmDataProvider(_cache, new GenericMdmDataProviderConfigurationWrapper(config), context.RequestId, context.receivedHeaders));
                }

                _dataProvidersCache.Add(key, dataProviderObject);
                return dataProviderObject;
            }
        }

        public List<DataProviderMetadata> GetMetadata()
        {

            return _dataProvidersCache.Values.Where(d => d != null && d.GetMetadata() != null)
                .Select(d => d.GetMetadata()).ToList();
        }
    }
}
