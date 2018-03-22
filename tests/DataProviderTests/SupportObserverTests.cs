using Diagnostics.DataProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Diagnostics.Tests.DataProviderTests
{
    public class SupportObserverTests
    {
        private SupportObserverDataProvider _dataProvider;
        public SupportObserverTests()
        {
            //setup
            var configurationFactory = new MockDataProviderConfigurationFactory();
            var configuration = configurationFactory.LoadConfigurations();
            _dataProvider = new SupportObserverDataProvider(null, configuration.SupportObserverConfiguration);
        }

        [Fact]
        public void GetRuntimeSlotMap()
        {
            var runtimeSlotMap = _dataProvider.GetRuntimeSiteSlotMap("waws-prod-bn1-71717c45", "thor-api").Result;
            Assert.True(runtimeSlotMap.ContainsKey("Production"));
        }

        [Fact]
        public void GetSiteObject()
        {
            var siteObject = _dataProvider.GetSite("thor-api").Result;
            Assert.Equal(siteObject.SiteName, "thor-api");
        }
    }
}
