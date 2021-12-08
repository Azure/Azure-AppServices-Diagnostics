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

    public class DataTableExceptionResponseObject
    {
        public IEnumerable<dynamic> Tables { get; set; }
        public IEnumerable<string> Exceptions { get; set; }
    }

    public class DataTableResponseObject
    {
        public string TableName { get; set; }

        public IEnumerable<DataTableResponseColumn> Columns { get; set; }

        public dynamic[][] Rows { get; set; }
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
        public dynamic[][] Rows { get; set; }
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

            for (int i = 0; i < dataTableResponse.Rows.GetLength(0); i++)
            {
                var row = dataTable.NewRow();
                for (int j = 0; j < dataTable.Columns.Count; j++)
                {
                    row[j] = dataTableResponse.Rows[i][j] ?? DBNull.Value;
                }

                dataTable.Rows.Add(row);
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

            for (int i = 0; i < appInsightsDataTableResponse.Rows.GetLength(0); i++)
            {
                var row = dataTable.NewRow();
                for (int j = 0; j < dataTable.Columns.Count; j++)
                {
                    row[j] = MaskPII(appInsightsDataTableResponse.Rows[i][j]) ?? DBNull.Value;
                }

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        // MaskPII for both internal and external users
        private static dynamic MaskPII(dynamic columnValue)
        {
            if (columnValue is string)
            {
                return ScriptUtilities.DataAnonymizer.AnonymizeContent(columnValue);
            }

            return columnValue;
        }

        public static DataTableResponseObject ToDataTableResponseObject(this DataTable table)
        {
            var dataTableResponseObject = new DataTableResponseObject
            {
                TableName = table.TableName
            };

            var columns = new List<DataTableResponseColumn>();
            foreach (DataColumn col in table.Columns)
            {
                columns.Add(new DataTableResponseColumn() { ColumnName = col.ColumnName, DataType = col.DataType.ToString().Replace("System.", "") });
            }

            var rows = new dynamic[table.Rows.Count][];

            for (int i = 0; i < table.Rows.Count; i++)
            {
                rows[i] = table.Rows[i].ItemArray;
            }

            dataTableResponseObject.Columns = columns;
            dataTableResponseObject.Rows = rows;

            return dataTableResponseObject;
        }

        internal static Type GetColumnType(string datatype)
        {
            if (datatype.Equals("int", StringComparison.OrdinalIgnoreCase))
            {
                datatype = "int32";
            }

            return Type.GetType($"System.{datatype}", false, true) ?? Type.GetType($"System.String");
        }
    }
}
