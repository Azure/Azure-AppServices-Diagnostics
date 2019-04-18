using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders
{
    internal class SupportObserverDataProviderFactory
    {
        public static ISupportObserverDataProvider GetDataProvider(OperationDataCache cache, DataSourcesConfiguration configuration, DataProviderContext dataProviderContext)
        {
            if (configuration.SupportObserverConfiguration.IsMockConfigured)
            {
                return new MockSupportObserverDataProvider(cache, configuration.SupportObserverConfiguration, dataProviderContext);
            }
            else
            {
                return new SupportObserverDataProvider(cache, configuration.SupportObserverConfiguration, dataProviderContext);
            }
        }
    }
}
