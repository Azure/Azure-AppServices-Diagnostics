using System;
using System.Collections.Generic;
using System.Text;

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
}
