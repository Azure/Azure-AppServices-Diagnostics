using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Diagnostics.DataProviders
{
    public static class DataTableUtility
    {
        public static DataTable GetDataTable(DataTableResponseObject dataTableResponse)
        {
            if (dataTableResponse == null)
            {
                throw new ArgumentNullException("dataTableResponse");
            }

            var dataTable = new DataTable(dataTableResponse.TableName);

            dataTable.Columns.AddRange(dataTableResponse.Columns.Select(column => new DataColumn(column.ColumnName, GetColumnType(column.ColumnType, column.DataType))).ToArray());

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

        public static DataTableResponseObject GetDataTableResponseObject(DataTable table)
        {
            var dataTableResponseObject = new DataTableResponseObject();

            dataTableResponseObject.TableName = table.TableName;

            var columns = new List<DataTableResponseColumn>();
            foreach(DataColumn col in table.Columns)
            {
                columns.Add(new DataTableResponseColumn() { ColumnName = col.ColumnName, DataType = col.DataType.ToString().Replace("System.", "") });
            }

            var rows = new List<string[]>();
            foreach(DataRow row in table.Rows)
            {
                rows.Add(row.ItemArray.Select(x => x.ToString()).ToArray());
            }

            dataTableResponseObject.Columns = columns;
            dataTableResponseObject.Rows = rows.ToArray();

            return dataTableResponseObject;
        }

        internal static Type GetColumnType(string type, string datatype)
        {
            return Type.GetType($"System.{datatype}");
        }
    }
}
