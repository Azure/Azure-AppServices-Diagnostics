using System.Data;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Diagnostics.Tests.DataProviderTests
{
    public class KustoDeserializationTests
    {
        [Fact]
        public void TestKustoDataTableConverter()
        {
            var dt = new DataTable();

            // When arrays or objects come from kusto they have column type 'object'
            // and come as types JArray and JObject respectively

            dt.Columns.Add("String", typeof(string));
            dt.Columns.Add("Array", typeof(object));
            dt.Columns.Add("Object", typeof(object));

            var arr = new JArray(new string[] { "test" });
            var obj = JObject.FromObject(new { Test = "Test" });

            dt.Rows.Add("test", arr, obj);

            var table = dt.ToDataTableResponseObject();

            var serialized = JsonConvert.SerializeObject(table);

            var deserialized = JsonConvert.DeserializeObject<DataTableResponseObject>(serialized);

            var dataTable = deserialized.ToDataTable();

            Assert.Equal(dt.Rows[0][0].ToString(), dataTable.Rows[0][0].ToString());

            Assert.Equal(((JArray)dt.Rows[0][1]).ToObject<string[]>(), ((JArray)dataTable.Rows[0][1]).ToObject<string[]>());

            Assert.Equal(dt.Rows[0][2], dataTable.Rows[0][2]);
        }
    }
}
