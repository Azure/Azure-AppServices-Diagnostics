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
        /// ResourceId for WAWSObserver AAD app
        /// </summary>
        public string ResourceId { get { return "d1abfd91-e19c-426e-802f-a6c55421a5ef"; } }
        /// <summary>
        /// Uri for SupportObserverResourceAAD app. 
        /// We are only hitting this API to access runtime site slot map data
        /// </summary>
        public string RuntimeSiteSlotMapResourceUri { get { return "https://microsoft.onmicrosoft.com/SupportObserverResourceApp"; } }

        public void PostInitialize()
        {
            //no op
        }
    }
}
