using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Diagnostics.ModelsAndUtils
{
    public class DataSummary
    {
        public string Name;

        public string Value;

        public string Color;

        public DataSummary(string name, string value)
        {
            this.Name = name ?? string.Empty; ;
            this.Value = value ?? string.Empty;
            this.Color = string.Empty;
        }

        public DataSummary(string name, string value, string color)
        {
            this.Name = name ?? string.Empty; ;
            this.Value = value ?? string.Empty;
            this.Color = color ?? string.Empty;
        }
    }

    public static class ResponseDataSummaryExtension
    {
        public static void AddDataSummary(this Response response, List<DataSummary> dataSummaryPoints)
        {
            if(dataSummaryPoints == null || !dataSummaryPoints.Any())
            {
                return;
            }

            List<DataTableResponseColumn> columns = PrepareColumnDefinitions();
            List<string[]> rows = new List<string[]>();

            dataSummaryPoints.ForEach(item =>
            {
                rows.Add(new List<string>()
                {
                    item.Name,
                    item.Value,
                    item.Color
                }.ToArray());
            });

            DataTableResponseObject table = new DataTableResponseObject()
            {
                Columns = columns,
                Rows = rows.ToArray()
            };

            response.Dataset.Add(new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.DataSummary)
            });
        }

        private static List<DataTableResponseColumn> PrepareColumnDefinitions()
        {
            List<DataTableResponseColumn> columnDefinitions = new List<DataTableResponseColumn>
            {
                new DataTableResponseColumn() { ColumnName = "Name" },
                new DataTableResponseColumn() { ColumnName = "Value" },
                new DataTableResponseColumn() { ColumnName = "Color" }
            };

            return columnDefinitions;
        }
    }
}
