using System;
using Diagnostics.DataProviders;
using Diagnostics.DataProviders.DataProviderConfigurations;

using Xunit;

namespace Diagnostics.Tests.DataProviderTests
{
    public class ChangeAnalysisDataProviderTest
    {
        [Fact]
        public async void TestChangeSetsRequest()
        {
            var dataSourceConfiguration = new MockDataProviderConfigurationFactory();
            var config = dataSourceConfiguration.LoadConfigurations();
            config.ChangeAnalysisDataProviderConfiguration = new ChangeAnalysisDataProviderConfiguration
            {
                AppKey = string.Empty,
                ClientId = string.Empty,
            };
            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config));

            // Throws exception when querying for changes beyond last 14 days.
            await Assert.ThrowsAsync<ArgumentException>(async () =>
             await dataProviders.ChangeAnalysis.GetChangeSetsForResource("/sites/test-site", DateTime.Now.AddDays(-15), DateTime.Now));
        }
    }
}
