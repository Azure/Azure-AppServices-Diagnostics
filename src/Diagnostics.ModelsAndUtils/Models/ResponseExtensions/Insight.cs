using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    /// <summary>
    /// Class representing Insight
    /// </summary>
    public class Insight
    {
        /// <summary>
        /// Enum reprensenting insight level.
        /// </summary>
        public InsightStatus Status;

        /// <summary>
        /// Insight Message.
        /// </summary>
        public string Message;

        /// <summary>
        /// Insights body.
        /// </summary>
        public Dictionary<string, string> Body;

        /// <summary>
        /// Creates an instance of Insight class.
        /// </summary>
        /// <param name="status">Enum reprensenting insight level.</param>
        /// <param name="message">Insight Message.</param>
        public Insight(InsightStatus status, string message)
        {
            this.Status = status;
            this.Message = message ?? string.Empty;
            this.Body = new Dictionary<string, string>();
        }

        /// <summary>
        /// Creates an instance of Insight class.
        /// </summary>
        /// <param name="status">Enum reprensenting insight level.</param>
        /// <param name="message">Insight Message.</param>
        /// <param name="body">Insights Body.</param>
        public Insight(InsightStatus status, string message, Dictionary<string, string> body)
        {
            this.Status = status;
            this.Message = message ?? string.Empty;
            this.Body = body;
        }
    }

    /// <summary>
    /// InsightStatus Enum
    /// </summary>
    public enum InsightStatus
    {
        Critical,
        Warning,
        Info,
        Success
    }

    public static partial class ResponseInsightsExtension
    {
        /// <summary>
        /// Adds a list of insights to response
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="insights">List of Insights</param>
        /// <example> 
        /// This sample shows how to use <see cref="AddInsights"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     Insight firstInsight = new Insight(InsightStatus.Critical, "insight1 title");
        ///     Insight secondInsight = new Insight(InsightStatus.Warning, "insight2 title");
        ///     
        ///     res.AddInsights(new List<![CDATA[<Insight>]]>(){ firstInsight, secondInsight });
        /// }
        /// </code>
        /// </example>
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

        /// <summary>
        /// Adds a single insight to response
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="insight">Insight</param>
        /// <example> 
        /// This sample shows how to use <see cref="AddInsight"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     Insight insight = new Insight(InsightStatus.Critical, "insight1 title");
        ///     
        ///     res.AddInsight(insight);
        /// }
        /// </code>
        /// </example>
        public static void AddInsight(this Response response, Insight insight)
        {
            if (insight == null) return;

            AddInsights(response, new List<Insight>() { insight });
        }

        /// <summary>
        /// Adds a single insight to response
        /// </summary>
        /// <param name="response">Response object.</param>
        /// <param name="status">Enum reprensenting insight level.</param>
        /// <param name="message">Insight Message.</param>
        /// <param name="body">Insights Body.</param>
        /// <example> 
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     res.AddInsight(InsightStatus.Critical, "insight1 title");
        /// }
        /// </code>
        /// </example>
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
