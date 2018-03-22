using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders
{
    internal class SupportObserverDataProviderFactory
    {
        public static ISupportObserverDataProvider GetDataProvider(OperationDataCache cache, DataSourcesConfiguration configuration)
        {
            if (configuration.SupportObserverConfiguration.IsMockConfigured)
            {
                return new MockSupportObserverDataProvider(cache);
            }
            else
            {
                return new SupportObserverDataProvider(cache, configuration.SupportObserverConfiguration);
            }
        }
    }
}
