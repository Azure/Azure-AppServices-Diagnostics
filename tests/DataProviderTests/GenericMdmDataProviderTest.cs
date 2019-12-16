using System.Collections.Generic;
using System.Collections.Immutable;
using Diagnostics.DataProviders;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Diagnostics.Tests.DataProviderTests
{
    public class GenericMdmDataProviderTest
    {
        [Fact]
        public void TestGenericSystemMdmAccount()
        {
            var dataSourceConfiguration = new MockDataProviderConfigurationFactory();

            var config = dataSourceConfiguration.LoadConfigurations();

            var mdmConfig = new GenericMdmDataProviderConfiguration
            {
                CertificateThumbprint = "BAD0BAD0BAD0BAD0BAD0BAD0BAD0BAD0BAD0BAD0",
                Endpoint = "http://0.0.0.0",
                MonitoringAccount = "Mock"
            };

            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config, incomingHeaders: new HeaderDictionary() { [HeaderConstants.LocationHeader] = "FakeLocation" }));

            var mdmDataProvider = dataProviders.MdmGeneric(mdmConfig);
            Assert.NotNull(mdmDataProvider);
        }

        [Fact]
        public async void TestDetectorWithMDMConfigurationGists()
        {
            var references = new Dictionary<string, string>
            {
                { "mdm", GetMDMConfigurationGist() },
            };

            var metadata = new EntityMetadata(GetMDMDetector(), EntityType.Detector);

            var dataSourceConfiguration = new MockDataProviderConfigurationFactory();

            var config = dataSourceConfiguration.LoadConfigurations();

            ArmResource resource = new ArmResource("751A8C1D-EA9D-4FE7-8574-3096A81C2C08", "testResourceGroup", "Microsoft.AppPlatform", "Spring", "testResource", "FakeLocation");
            OperationContext<ArmResource> context = new OperationContext<ArmResource>(resource, "2019-12-09T00:10", "2019-12-09T23:54", true, "A9854948-807B-4371-B834-3EC78BB6635C");
            Response response = new Response();

            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config, incomingHeaders: new HeaderDictionary() { [HeaderConstants.LocationHeader] = resource.Location }));

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports(), references.ToImmutableDictionary()))
            {
                await invoker.InitializeEntryPointAsync().ConfigureAwait(false);
                await invoker.Invoke(new object[] { dataProviders, context, response }).ConfigureAwait(false);
                Assert.Equal("Diagnostics.DataProviders.MdmLogDecorator", response.Insights[0].Message);
            }
        }

        private string GetMDMConfigurationGist() =>
@"
using Diagnostics.DataProviders.DataProviderConfigurations;

public static class MDMConfiguration
{
    public static GenericMdmDataProviderConfiguration GetConfiguration() =>
        new GenericMdmDataProviderConfiguration()
        {
            CertificateThumbprint = ""BAD0BAD0BAD0BAD0BAD0BAD0BAD0BAD0BAD0BAD0"",
            Endpoint = ""http://0.0.0.0"",
            MonitoringAccount = ""Mock""
        };
}
";

        private string GetMDMDetector() =>
@"
#load ""mdm""

using System;

[AppFilter(AppType = AppType.All, PlatformType = PlatformType.Windows, StackType = StackType.All)]
[Definition(Id = ""id"", Name = ""name"", Author = ""authors"", Description = ""description"", Category = """")]
public async static Task<Response> Run(DataProviders dp, OperationContext<ArmResource> cxt, Response res)
{
    var resp = dp.MdmGeneric(MDMConfiguration.GetConfiguration()).GetType().ToString();
    res.AddInsight(InsightStatus.Info, resp);
    return await Task.FromResult(res);
}
";
    }
}
