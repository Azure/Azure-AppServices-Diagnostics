using Diagnostics.DataProviders;
using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Diagnostics.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config));

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();

                var appResource = new App(string.Empty, string.Empty, "my-api")
                {
                    Stamp = new HostingEnvironment(string.Empty, string.Empty, "waws-prod-bn1-71717c45")
                    {
                        Name = "waws-prod-bn1-71717c45"
                    }
                };

                var operationContext = new OperationContext<App>(appResource, string.Empty, string.Empty, true, string.Empty);
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

            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config));

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();

                var appResource = new App(string.Empty, string.Empty, "my-api")
                {
                    Stamp = new HostingEnvironment(string.Empty, string.Empty, "waws-prod-bn1-71717c45")
                };

                appResource.Stamp.TenantIdList = new List<string>()
                {
                    Guid.NewGuid().ToString()
                };

                var operationContext = new OperationContext<App>(appResource, null, null, true, null);

                var response = new Response();

                try
                {
                    Response result = (Response)await invoker.Invoke(new object[] { dataProviders, operationContext, response });
                }
                catch (ScriptCompilationException ex)
                {
                    foreach(var output in ex.CompilationOutput)
                    {
                        Trace.WriteLine(output);
                    }
                }

                
            }
        }

        [Fact]
        public async void TestBadObserverUrl()
        {
            var configFactory = new MockDataProviderConfigurationFactory();
            var config = configFactory.LoadConfigurations();
            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config));

            try
            {
                var data = await dataProviders.Observer.GetResource("https://not-wawsobserver.azurewebsites.windows.net/Sites/thor-api");
            }catch(Exception ex)
            {
                Assert.Contains("Please use a URL that points to one of the hosts", ex.Message);
            }

            await Assert.ThrowsAsync<FormatException>(async() => await dataProviders.Observer.GetResource("/sites/hawfor-site"));

            try
            {
                var data3 = await dataProviders.Observer.GetResource("/not-a-route/hawfor-site/not-resource");
            }catch(FormatException ex)
            {
                Assert.Contains("Please use a URL that points to one of the hosts", ex.Message);
            }
            
            await Assert.ThrowsAsync<ArgumentNullException>(async() => await dataProviders.Observer.GetResource(null));
        }

        [Fact]
        public async void TestRouteMatchLogic()
        {
            var configFactory = new MockDataProviderConfigurationFactory();
            var config = configFactory.LoadConfigurations();
            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config));

            //basic test
            var site123Data = await dataProviders.Observer.GetResource("https://wawsobserver.azurewebsites.windows.net/stamps/stamp123/sites/site123");

            //basic test with different resource
            var certData = await dataProviders.Observer.GetResource("https://wawsobserver.azurewebsites.windows.net/certificates/123456789");

            //test where parameter value is middle of url and not end of url
            var domainData = await dataProviders.Observer.GetResource("https://wawsobserver.azurewebsites.windows.net/subscriptions/1111-2222-3333-4444-5555/domains");

            //test with multiple parameter values
            var storageVolumeData = await dataProviders.Observer.GetResource("https://wawsobserver.azurewebsites.windows.net/stamps/waws-prod-mock1-001/storagevolumes/volume-19");

            Assert.Equal("site123", (string)site123Data.siteName);
            Assert.Equal("IPSSL", (string)certData.type);
            Assert.Equal("foo_bar.com", (string)domainData[0].name);
            Assert.Equal("volume-19", (string)storageVolumeData.name);
        }
    }
}
