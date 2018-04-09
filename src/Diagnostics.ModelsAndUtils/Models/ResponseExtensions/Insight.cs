using System;
using System.Collections.Generic;
using System.Data;
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
        public static DiagnosticData AddInsights(this Response response, List<Insight> insights, string title = null, string description = null)
        {
            if (insights == null || !insights.Any()) return null;

            DataTable table = new DataTable();

            table.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("Status", typeof(string)),
                new DataColumn("Message", typeof(string)),
                new DataColumn("Data.Name", typeof(string)),
                new DataColumn("Data.Value", typeof(string))
            });

            insights.ForEach(insight =>
            {
                if (insight.Body == null || !insight.Body.Keys.Any())
                {
                    table.Rows.Add(new string[]
                    {
                        insight.Status.ToString(),
                        insight.Message,
                        string.Empty,
                        string.Empty
                    });
                }
                else
                {
                    foreach (KeyValuePair<string, string> entry in insight.Body)
                    {
                        table.Rows.Add(new string[]
                        {
                            insight.Status.ToString(),
                            insight.Message,
                            entry.Key,
                            entry.Value
                        });

                    }
                }
            });

            var diagnosticData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Insights)
                {
                    Title = title,
                    Description = description
                }
            };

            response.Dataset.Add(diagnosticData);

            return diagnosticData;
        }

        public static DiagnosticData AddInsight(this Response response, Insight insight)
        {
            if (insight == null) return null;

            return AddInsights(response, new List<Insight>() { insight });
        }

        public static void AddInsight(this Response response, InsightStatus status, string message, Dictionary<string, string> body = null)
        {
            AddInsight(response, new Insight(status, message, body));
        }
    }
}
