using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.DataProviders;
using Diagnostics.DataProviders.Exceptions;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Diagnostics.Tests.Helpers;
using Xunit;

namespace Diagnostics.Tests.DataProviderTests
{
    public class SupportObserverTests
    {
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

                var operationContext = new OperationContext<App>(appResource, null, null, true, null);

                var response = new Response();

                try
                {
                    Response result = (Response)await invoker.Invoke(new object[] { dataProviders, operationContext, response });
                }
                catch (ScriptCompilationException ex)
                {
                    foreach (var output in ex.CompilationOutput)
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
            }
            catch (Exception ex)
            {
                Assert.Contains("Please use a URL that points to one of the hosts", ex.Message);
            }

            await Assert.ThrowsAsync<FormatException>(async () => await dataProviders.Observer.GetResource("/sites/hawfor-site"));

            try
            {
                var data3 = await dataProviders.Observer.GetResource("/not-a-route/hawfor-site/not-resource");
            }
            catch (FormatException ex)
            {
                Assert.Contains("Please use a URL that points to one of the hosts", ex.Message);
            }

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await dataProviders.Observer.GetResource(null));
        }

        [Fact]
        public async void TestObserverDeprecatedResponse()
        {
            var configFactory = new MockDataProviderConfigurationFactory();
            var config = configFactory.LoadConfigurations();
            config.SupportObserverConfiguration.UnsupportedApis = "^api\\/stamps\\/.*\\/sites\\/[^\\/]*.$;^api\\/sites\\/[^\\/]*.$";
            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config));

            //the following calls are deprecated
            await Assert.ThrowsAsync<ApiNotSupportedException>(async () => await dataProviders.Observer.GetSite("my-site"));
            await Assert.ThrowsAsync<ApiNotSupportedException>(async () => await dataProviders.Observer.GetSite("my-stamp", "my-site"));
            await Assert.ThrowsAsync<ApiNotSupportedException>(async () => await dataProviders.Observer.GetResource("https://wawsobserver.azurewebsites.windows.net/sites/my-site"));
            await Assert.ThrowsAsync<ApiNotSupportedException>(async () => await dataProviders.Observer.GetResource("https://wawsobserver.azurewebsites.windows.net/stamps/my-stamp/sites/my-site"));

            //making sure other APIs are not affected by the regex match
            try
            {
                await dataProviders.Observer.GetResource("https://wawsobserver.azurewebsites.windows.net/stamps/my-stamp/sites/my-site/adminsites");
                await dataProviders.Observer.GetResource("https://wawsobserver.azurewebsites.windows.net/sites/my-site/adminsites");
            }
            catch (NotSupportedException)
            {
                Assert.True(false, "These calls should not give NotSupportedException");
            }
        }

        internal async void TestObserver()
        {
            var dataSourceConfiguration = new MockDataProviderConfigurationFactory();
            var config = dataSourceConfiguration.LoadConfigurations();
            config.SupportObserverConfiguration = new SupportObserverDataProviderConfiguration()
            {
                AppKey = "",
                ClientId = "",
                IsMockConfigured = false
            };

            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config));
            var wawsObserverData = await dataProviders.Observer.GetResource("https://wawsobserver.azurewebsites.windows.net/sites/highcpuscenario");
            var supportBayData = await dataProviders.Observer.GetResource("https://support-bay-api.azurewebsites.net/observer/stamps/waws-prod-bay-073/sites/highcpuscenario/postbody");

            Assert.True(wawsObserverData != null);
            Assert.True(supportBayData != null);
        }
    }
}
