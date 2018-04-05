using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Diagnostics.ModelsAndUtils
{
    public class Insight
    {
        public InsightStatus Status;

        public string Message;

        public Dictionary<string, string> Body;

        public Insight(InsightStatus status, string message)
        {
            this.Status = status;
            this.Message = message ?? string.Empty;
            this.Body = new Dictionary<string, string>();
        }

        public Insight(InsightStatus status, string message, Dictionary<string, string> body)
        {
            this.Status = status;
            this.Message = message ?? string.Empty;
            this.Body = body;
        }
    }

    public enum InsightStatus
    {
        Critical,
        Warning,
        Info,
        Success
    }

    public static class ResponseInsightsExtension
    {
        public static void AddInsights(this Response response, List<Insight> insights)
        {
            if (insights == null || !insights.Any()) return;

            List<DataTableResponseColumn> columns = PrepareColumnDefinitions();
            List<string[]> rows = new List<string[]>();

            insights.ForEach(insight =>
            {
                if (insight.Body == null || !insight.Body.Keys.Any())
                {
                    List<string> dataRow = new List<string>
                    {
                        insight.Status.ToString(),
                        insight.Message,
                        string.Empty,
                        string.Empty
                    };
                    rows.Add(dataRow.ToArray());
                }
                else
                {
                    foreach (KeyValuePair<string, string> entry in insight.Body)
                    {
                        List<string> dataRow = new List<string>
                        {
                            insight.Status.ToString(),
                            insight.Message,
                            entry.Key,
                            entry.Value
                        };

                        rows.Add(dataRow.ToArray());
                    }
                }
            });

            DataTableResponseObject table = new DataTableResponseObject()
            {
                Columns = columns,
                Rows = rows.ToArray()
            };

            response.Dataset.Add(new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Insights)
            });
        }

        public static void AddInsight(this Response response, Insight insight)
        {
            if (insight == null) return;

            AddInsights(response, new List<Insight>() { insight });
        }

        public static void AddInsight(this Response response, InsightStatus status, string message, Dictionary<string, string> body = null)
        {
            AddInsight(response, new Insight(status, message, body));
        }

        private static List<DataTableResponseColumn> PrepareColumnDefinitions()
        {
            List<DataTableResponseColumn> columnDefinitions = new List<DataTableResponseColumn>
            {
                new DataTableResponseColumn() { ColumnName = "Status" },
                new DataTableResponseColumn() { ColumnName = "Message" },
                new DataTableResponseColumn() { ColumnName = "Data.Name" },
                new DataTableResponseColumn() { ColumnName = "Data.Value" }
            };

            return columnDefinitions;
        }
    }
}
