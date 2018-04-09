using System;
using System.Collections.Generic;
using System.Data;
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
        public static DiagnosticData AddDataSummary(this Response response, List<DataSummary> dataSummaryPoints, string title = null, string description = null)
        {
            if(dataSummaryPoints == null || !dataSummaryPoints.Any())
            {
                return null;
            }

            DataTable table = new DataTable("Data Summary");

            table.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("Name", typeof(string)),
                new DataColumn("Value", typeof(string)),
                new DataColumn("Color", typeof(string))
            });

            dataSummaryPoints.ForEach(item =>
            {
                table.Rows.Add(new string[]
                {
                    item.Name,
                    item.Value,
                    item.Color
                });
            });

            var summaryData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.DataSummary)
                {
                    Title = title,
                    Description = description
                }
            };

            response.Dataset.Add(summaryData);

            return summaryData;
        }
    }
}
