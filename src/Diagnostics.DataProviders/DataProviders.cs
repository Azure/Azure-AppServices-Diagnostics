using Diagnostics.DataProviders.Interfaces;
using System;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Data providers
    /// </summary>
    public class DataProviders
    {
        private OperationDataCache _cache = new OperationDataCache();

        /// <summary>
        /// Gets Kusto data provider
        /// </summary>
        public IKustoDataProvider Kusto { get; private set; }

        /// <summary>
        /// Gets Observer data provider
        /// </summary>
        public ISupportObserverDataProvider Observer { get; private set; }

        /// <summary>
        /// Gets Geo master data provider
        /// </summary>
        public IGeoMasterDataProvider GeoMaster { get; private set; }

        /// <summary>
        /// Gets App insights data provider
        /// </summary>
        public IAppInsightsDataProvider AppInsights { get; private set; }

        /// <summary>
        /// Gets Mdm data provider
        /// </summary>
        public IMdmDataProvider Mdm { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataProviders" /> class.
        /// </summary>
        /// <param name="context">Data provider context</param>
        public DataProviders(DataProviderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            Kusto = new DataProviderLogDecorator(context, new KustoDataProvider(_cache, context.Configuration.KustoConfiguration, context.RequestId));
            Observer = new DataProviderLogDecorator(context, SupportObserverDataProviderFactory.GetDataProvider(_cache, context.Configuration));
            GeoMaster = new DataProviderLogDecorator(context, new GeoMasterDataProvider(_cache, context.Configuration.GeoMasterConfiguration));
            AppInsights = new DataProviderLogDecorator(context, new AppInsightsDataProvider(_cache, context.Configuration.AppInsightsConfiguration));
            Mdm = new DataProviderLogDecorator(context, new MdmDataProvider(_cache, context.Configuration.MdmConfiguration, context.RequestId));
        }
    }
}
