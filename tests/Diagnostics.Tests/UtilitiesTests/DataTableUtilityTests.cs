using System;
using System.Data;
using System.Linq;
using Diagnostics.ModelsAndUtils.Models;

using Xunit;

namespace Diagnostics.Tests
{
    public class DataTableUtilityTests
    {
        [Fact]
        public void TestDataTableToDataTableResponseObject()
        {
            var table = new DataTable();
            table.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("SampleString", typeof(string)),
                new DataColumn("SampleDateTime", typeof(DateTime)),
                new DataColumn("SampleInt", typeof(int))
            });

            table.Rows.Add(new object[] { "Sample String", DateTime.Now, 32 });

            var convertedTable = table.ToDataTableResponseObject();

            var columns = convertedTable.Columns.ToArray();

            Assert.Equal("String", columns[0].DataType);
            Assert.Equal("DateTime", columns[1].DataType);
            Assert.Equal("Int32", columns[2].DataType);

            Assert.Single(convertedTable.Rows);
            Assert.Equal<int>(3, convertedTable.Rows[0].Length);
        }

        [Fact]
        public void TestDataTableToDataTableResponseObjectAndBack()
        {
            var table = new DataTable();
            table.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("SampleString", typeof(string)),
                new DataColumn("SampleDateTime", typeof(DateTime)),
                new DataColumn("SampleInt", typeof(int))
            });

            var now = DateTime.Now;
            var dateTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);

            table.Rows.Add(new object[] { "Sample String", dateTime, 32 });

            var dataTableResponseObject = table.ToDataTableResponseObject();

            var dataTableConvertedBack = dataTableResponseObject.ToDataTable();

            for (int i = 0; i < table.Columns.Count; i++)
            {
                Assert.Equal(table.Columns[i].ColumnName, dataTableConvertedBack.Columns[i].ColumnName);
                Assert.Equal(table.Columns[i].DataType, dataTableConvertedBack.Columns[i].DataType);
            }

            var expectedFirstRow = table.Rows[0].ItemArray;
            var actualFirstRow = dataTableConvertedBack.Rows[0].ItemArray;

            Assert.Equal(expectedFirstRow.Length, actualFirstRow.Length);

            for (int i = 0; i < expectedFirstRow.Length; i++)
            {
                Assert.Equal(expectedFirstRow[i], actualFirstRow[i]);
            }
        }
    }
}
