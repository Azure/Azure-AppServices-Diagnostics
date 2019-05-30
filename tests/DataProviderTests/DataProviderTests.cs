using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Diagnostics.DataProviders;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using Diagnostics.Scripts;
using Diagnostics.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Diagnostics.Tests.DataProviderTests
{
    /// <summary>
    /// Data provider tests
    /// </summary>
    public class DataProviderTests
    {
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="ITestOutputHelper" /> class.
        /// </summary>
        /// <param name="output">Test output</param>
        public DataProviderTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Mdm data provider test
        /// </summary>
        [Fact]
        public async void TestMdmGetNamespaceAsync()
        {
            var metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = @"
                public async static Task<IEnumerable<string>> Run(DataProviders dataProviders) {
                    return await dataProviders.Mdm(MdmDataSource.Antares).GetNamespacesAsync();
                }";

            var configFactory = new MockDataProviderConfigurationFactory();
            var config = configFactory.LoadConfigurations();

            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config, Guid.NewGuid().ToString()));

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                var result = (await invoker.Invoke(new object[] { dataProviders })) as IEnumerable<string>;

                Assert.NotNull(result);
                Assert.True(result.Count() == 3);
            }
        }

        /// <summary>
        /// Mdm data provider test
        /// </summary>
        [Fact]
        public async void TestMdmGetMetricNamesAsync()
        {
            var metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = @"
                public async static Task<IEnumerable<string>> Run(DataProviders dataProviders) {
                    return await dataProviders.Mdm(MdmDataSource.Antares).GetMetricNamesAsync(""Microsoft/Web/WebApps"");
                }";

            var configFactory = new MockDataProviderConfigurationFactory();
            var config = configFactory.LoadConfigurations();

            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config, Guid.NewGuid().ToString()));

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                var result = await invoker.Invoke(new object[] { dataProviders }) as IEnumerable<string>;

                Assert.NotNull(result);
                Assert.True(result.Count() == 3);
            }
        }

        /// <summary>
        /// Mdm data provider test
        /// </summary>
        [Fact]
        public async void TestMdmGetDimensionNamesAsync()
        {
            var metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = @"
                public async static Task<IEnumerable<string>> Run(DataProviders dataProviders) {
                    return await dataProviders.Mdm(MdmDataSource.Antares).GetDimensionNamesAsync(""Microsoft/Web/WebApps"", ""CpuTime"");
                }";

            var configFactory = new MockDataProviderConfigurationFactory();
            var config = configFactory.LoadConfigurations();

            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config, Guid.NewGuid().ToString()));

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                var result = await invoker.Invoke(new object[] { dataProviders }) as IEnumerable<string>;

                Assert.NotNull(result);
                Assert.True(result.Count() == 3);
            }
        }

        /// <summary>
        /// Mdm data provider test
        /// </summary>
        [Fact]
        public async void TestMdmGetDimensionValuesAsync()
        {
            var metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = @"
                public async static Task<IEnumerable<string>> Run(DataProviders dataProviders) {
                    var filter = new List<Tuple<string, IEnumerable<string>>>
                    {
                        new Tuple<string, IEnumerable<string>>(""StampName"", new List<string>())
                    };

                    return await dataProviders.Mdm(MdmDataSource.Antares).GetDimensionValuesAsync(""Microsoft/Web/WebApps"", ""CpuTime"", filter, ""ServerName"", DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow);
                }";

            var configFactory = new MockDataProviderConfigurationFactory();
            var config = configFactory.LoadConfigurations();

            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config, Guid.NewGuid().ToString()));

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                var result = await invoker.Invoke(new object[] { dataProviders }) as IEnumerable<string>;

                Assert.NotNull(result);
                Assert.True(result.Count() == 3);
            }
        }

        /// <summary>
        /// Mdm data provider test
        /// </summary>
        [Fact]
        public async void TestMdmGetTimeSeriesValuesAsync()
        {
            var metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = @"
                public async static Task<IEnumerable<DataTable>> Run(DataProviders dataProviders) {
                    var dimensions = new Dictionary<string, string> { { ""StampName"", ""kudu1"" } };
                    return await dataProviders.Mdm(MdmDataSource.Antares).GetTimeSeriesAsync(DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow, Sampling.Average | Sampling.Max | Sampling.Count, ""Microsoft/Web/WebApps"", ""CpuTime"", dimensions);
                }";

            var configFactory = new MockDataProviderConfigurationFactory();
            var config = configFactory.LoadConfigurations();

            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config, Guid.NewGuid().ToString()));

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                var result = await invoker.Invoke(new object[] { dataProviders }) as IEnumerable<DataTable>;

                Assert.NotNull(result);
            }
        }

        /// <summary>
        /// Kusto data provider test
        /// </summary>
        [Fact]
        public async void DataProvders_TestKusto()
        {
            var metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = @"
                public async static Task<DataTable> Run(DataProviders dataProviders) {
                    return await dataProviders.Kusto.ExecuteQuery(""TestA"", ""waws-prod-mockstamp"");
                }";

            var configFactory = new MockDataProviderConfigurationFactory();
            var config = configFactory.LoadConfigurations();

            var dataProviders = new DataProviders.DataProviders(new DataProviderContext(config, Guid.NewGuid().ToString()));

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                var result = (DataTable)await invoker.Invoke(new object[] { dataProviders });

                Assert.NotNull(result);
            }
        }

        private void PrintDataTable(DataTable dt)
        {
            var cols = new StringBuilder();
            foreach (DataColumn column in dt.Columns)
            {
                cols.Append($"{column.ColumnName}\t");
            }

            output.WriteLine(cols.ToString());

            foreach (DataRow row in dt.Rows)
            {
                var sb = new StringBuilder();
                foreach (DataColumn column in dt.Columns)
                {
                    sb.Append($"{row[column.ColumnName]}\t");
                }

                output.WriteLine(sb.ToString());
            }
        }
    }
}
