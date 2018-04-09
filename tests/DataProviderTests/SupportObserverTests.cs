using Diagnostics.DataProviders;
using Diagnostics.ModelsAndUtils;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Diagnostics.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Diagnostics.Tests.DataProviderTests
{
    public class SupportObserverTests
    {
        public SupportObserverTests()
        {
        }

        [Fact]
        public async void E2E_Test_RuntimeSlotMapData()
        {
            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            //read a sample csx file from local directory
            metadata.ScriptText = await File.ReadAllTextAsync("GetRuntimeSiteSlotMapData.csx");

            var configFactory = new MockDataProviderConfigurationFactory();
            var config = configFactory.LoadConfigurations();

            var dataProviders = new DataProviders.DataProviders(config);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();

                var siteResource = new SiteResource
                {
                    SiteName = "my-api",
                    Stamp = "waws-prod-bn1-71717c45"
                };
                var operationContext = new OperationContext(siteResource, null, null);
                var response = new Response();

                Response result = (Response)await invoker.Invoke(new object[] { dataProviders, operationContext, response });

                Assert.Equal("my-api__a88nf", result.Dataset.First().Table.Rows[1][1]);
            }
        }

        [Fact]
        public async Task E2E_Test_WAWSObserverAsync()
        {
            var configFactory = new MockDataProviderConfigurationFactory();
            var config = configFactory.LoadConfigurations();

            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            //read a sample csx file from local directory
            metadata.ScriptText = await File.ReadAllTextAsync("BackupCheckDetector.csx");

            var dataProviders = new DataProviders.DataProviders(config);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();

                var siteResource = new SiteResource
                {
                    SiteName = "my-api",
                    Stamp = "waws-prod-bn1-71717c45",
                    TenantIdList = new List<string>()
                    {
                        Guid.NewGuid().ToString()
                    }
                };

                var operationContext = new OperationContext(siteResource, null, null);

                var response = new Response();

                Response result = (Response)await invoker.Invoke(new object[] { dataProviders, operationContext, response });
            }
        }
    }
}
