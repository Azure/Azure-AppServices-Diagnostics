using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class DataTableResponseObjectCollection
    {
        public IEnumerable<DataTableResponseObject> Tables { get; set; }
    }

    public class DataTableResponseObject
    {
        public string TableName { get; set; }

        public IEnumerable<DataTableResponseColumn> Columns { get; set; }

        public string[][] Rows { get; set; }
    }

    public class DataTableResponseColumn
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string ColumnType { get; set; }
    }

    public class AppInsightsDataTableResponseObjectCollection
    {
        public IEnumerable<AppInsightsDataTableResponseObject> Tables { get; set; }
    }

    public class AppInsightsDataTableResponseObject
    {
        public string Name { get; set; }
        public IEnumerable<AppInsightsDataTableResponseColumn> Columns { get; set; }
        public string[][] Rows { get; set; }
    }

    public class AppInsightsDataTableResponseColumn
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public static class DataTableExtensions
    {
        public static DataTable ToDataTable(this DataTableResponseObject dataTableResponse)
        {
            if (dataTableResponse == null)
            {
                throw new ArgumentNullException("dataTableResponse");
            }

            var dataTable = new DataTable(dataTableResponse.TableName);

            dataTable.Columns.AddRange(dataTableResponse.Columns.Select(column => new DataColumn(column.ColumnName, GetColumnType(column.DataType))).ToArray());

            foreach (var row in dataTableResponse.Rows)
            {
                var rowWithCorrectTypes = new List<object>();
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    object rowValueWithCorrectType = null;

                    if (row[i] != null)
                    {
                        rowValueWithCorrectType = Convert.ChangeType(row[i], dataTable.Columns[i].DataType);
                    }

                    rowWithCorrectTypes.Add(rowValueWithCorrectType);
                }

                dataTable.Rows.Add(rowWithCorrectTypes.ToArray());
            }

            return dataTable;
        }

        public static DataTable ToAppInsightsDataTable(this AppInsightsDataTableResponseObject appInsightsDataTableResponse)
        {
            if (appInsightsDataTableResponse == null)
            {
                throw new ArgumentNullException("appInsightsDataTableResponse");
            }

            var dataTable = new DataTable(appInsightsDataTableResponse.Name);
            dataTable.Columns.AddRange(appInsightsDataTableResponse.Columns.Select(column => new DataColumn(column.Name, GetColumnType(column.Type))).ToArray());

            foreach (var row in appInsightsDataTableResponse.Rows)
            {
                var rowWithCorrectTypes = new List<object>();
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    object rowValueWithCorrectType = null;

                    if (row[i] != null)
                    {
                        rowValueWithCorrectType = Convert.ChangeType(row[i], dataTable.Columns[i].DataType);
                    }

                    rowWithCorrectTypes.Add(rowValueWithCorrectType);
                }

                dataTable.Rows.Add(rowWithCorrectTypes.ToArray());

            }

            return dataTable;
        }

        public static DataTableResponseObject ToDataTableResponseObject(this DataTable table)
        {
            var dataTableResponseObject = new DataTableResponseObject();

            dataTableResponseObject.TableName = table.TableName;

            var columns = new List<DataTableResponseColumn>();
            foreach (DataColumn col in table.Columns)
            {
                columns.Add(new DataTableResponseColumn() { ColumnName = col.ColumnName, DataType = col.DataType.ToString().Replace("System.", "") });
            }

            var rows = new List<string[]>();
            foreach (DataRow row in table.Rows)
            {
                rows.Add(row.ItemArray.Select(x => x.ToString()).ToArray());
            }

            dataTableResponseObject.Columns = columns;
            dataTableResponseObject.Rows = rows.ToArray();

            return dataTableResponseObject;
        }

        internal static Type GetColumnType(string datatype)
        {
            datatype = datatype.Equals("int", StringComparison.OrdinalIgnoreCase) ? "int32" : datatype;
            datatype = datatype.Equals("dynamic", StringComparison.OrdinalIgnoreCase) ? "string" : datatype;
            return Type.GetType($"System.{datatype}", false, true);
        }
    }
}
