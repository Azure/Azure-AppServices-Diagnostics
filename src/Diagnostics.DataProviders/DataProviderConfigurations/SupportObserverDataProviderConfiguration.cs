using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders
{
    [DataSourceConfiguration("SupportObserver")]
    public class SupportObserverDataProviderConfiguration : IDataProviderConfiguration
    {
        public SupportObserverDataProviderConfiguration()
        {
        }

        /// <summary>
        /// Client Id
        /// </summary>
        [ConfigurationName("ClientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// App Key
        /// </summary>
        [ConfigurationName("AppKey")]
        public string AppKey { get; set; }

        [ConfigurationName("IsProdConfigured", DefaultValue = true)]
        public bool IsProdConfigured { get; set; }

        [ConfigurationName("IsTestConfigured", DefaultValue = false)]
        public bool IsTestConfigured { get; set; }

        [ConfigurationName("IsMockConfigured", DefaultValue = false)]
        public bool IsMockConfigured { get; set; }

        /// <summary>
        /// Uri for SupportObserverResourceAAD app
        /// </summary>
        public string ResourceUri { get { return "https://microsoft.onmicrosoft.com/SupportObserverResourceApp"; } }

        public void PostInitialize()
        {
            //no op
        }
    }
}
