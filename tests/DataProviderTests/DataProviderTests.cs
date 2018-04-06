using Diagnostics.DataProviders;
using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Diagnostics.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Diagnostics.Tests.DataProviderTests
{
    public class DataProviderTests
    {
        private readonly ITestOutputHelper output;

        public DataProviderTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async void DataProvders_TestKusto()
        {
            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = GetDataProviderScript("TestA");

            var configFactory = new MockDataProviderConfigurationFactory();
            var config = configFactory.LoadConfigurations();

            var dataProviders = new DataProviders.DataProviders(config);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                DataTableResponseObject result = (DataTableResponseObject)await invoker.Invoke(new object[] { dataProviders });

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

        public static string GetDataProviderScript(string test)
        {
            switch (test)
            {
                case "TestA":
                default:
                    return @"
                       
                        public async static Task<DataTableResponseObject> Run(DataProviders dataProviders) {

                            var dt = await dataProviders.Kusto.ExecuteQuery(""TestA"", string.Empty);
                            return dt;
                    }";
            }
        }
    }
}
