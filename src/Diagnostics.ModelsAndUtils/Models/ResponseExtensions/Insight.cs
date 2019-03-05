using Diagnostics.ModelsAndUtils.ScriptUtilities;
using Diagnostics.ModelsAndUtils.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
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
        /// Enum representing insight level.
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
        /// Whether insight is expanded to begin with
        /// </summary>
        public bool IsExpanded;

        /// <summary>
        /// Optional solutions to common problems to which the Insight may refer
        /// </summary>
        public IEnumerable<Solution> Solutions;

        /// <summary>
        /// Creates an instance of Insight class.
        /// </summary>
        /// <param name="status">Enum reprensenting insight level.</param>
        /// <param name="message">Insight Message.</param>
        /// <param name="solutions">Solutions to problems identified by the Insight.</param>
        public Insight(InsightStatus status, string message)
        {
            this.Status = status;
            this.Message = message ?? string.Empty;
            this.Body = new Dictionary<string, string>();
            this.IsExpanded = false;
        }

        /// <summary>
        /// Creates an instance of Insight class.
        /// </summary>
        /// <param name="status">Enum reprensenting insight level.</param>
        /// <param name="message">Insight Message.</param>
        /// <param name="body">Insights Body.</param>
        /// <param name="solutions">Solutions to problems identified by the Insight.</param>
        public Insight(InsightStatus status, string message, Dictionary<string, string> body)
        {
            this.Status = status;
            this.Message = message ?? string.Empty;
            this.Body = body;
            this.IsExpanded = false;
        }

        /// <summary>
        /// Creates an instance of Insight class.
        /// </summary>
        /// <param name="status">Enum reprensenting insight level.</param>
        /// <param name="message">Insight Message.</param>
        /// <param name="body">Insights Body.</param>
        /// <param name="solutions">Solutions to problems identified by the Insight.</param>
        public Insight(InsightStatus status, string message, Dictionary<string, string> body, bool isExpanded) :
            this(status, message, body)
        {
            this.IsExpanded = isExpanded;
        }

        /// <summary>
        /// Chain <see cref="Solution"/>s onto existing insights
        /// </summary>
        public Insight WithSolutions(params Solution[] solutions)
        {
            Solutions = (Solutions ?? new List<Solution>()).Concat(solutions);

            return this;
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
        Success,
        None
    }

    public static partial class ResponseInsightsExtension
    {
        /// <summary>
        /// Adds a list of insights to response
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="insights">List of Insights</param>
        /// <returns>Diagnostic Data object that represents insights</returns>
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
        public static DiagnosticData AddInsights(this Response response, List<Insight> insights, string title = null, string description = null)
        {
            if (insights == null || !insights.Any())
            {
                return null;
            }

            DataTable table = new DataTable();

            table.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("Status", typeof(string)),
                new DataColumn("Message", typeof(string)),
                new DataColumn("Data.Name", typeof(string)),
                new DataColumn("Data.Value", typeof(string)),
                new DataColumn("Expanded", typeof(string)),
                new DataColumn(nameof(Insight.Solutions), typeof(string))
            });

            insights.ForEach(insight =>
            {
                foreach (var solution in insight.Solutions ?? Enumerable.Empty<Solution>())
                {
                    solution.DetectorLink = UriUtilities.BuildDetectorUri(solution.ResourceUri, response.Metadata.Id);
                    solution.IsInternal = response.IsInternalCall;
                }

                if (insight.Body == null || !insight.Body.Keys.Any())
                {
                    table.Rows.Add(new string[]
                    {
                        insight.Status.ToString(),
                        insight.Message,
                        string.Empty,
                        string.Empty,
                        insight.IsExpanded.ToString(),
                        JsonConvert.SerializeObject(insight.Solutions)
                    });
                }
                else
                {
                    // This duplicates all other insight properties for each KVP in Body
                    foreach (KeyValuePair<string, string> entry in insight.Body)
                    {
                        table.Rows.Add(new string[]
                        {
                            insight.Status.ToString(),
                            insight.Message,
                            entry.Key,
                            entry.Value,
                            insight.IsExpanded.ToString(),
                            JsonConvert.SerializeObject(insight.Solutions)
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

            response.Insights.AddRange(insights);

            return diagnosticData;
        }

        /// <summary>
        /// Adds a single insight to response
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="insight">Insight</param>
        /// <returns>Diagnostic Data object that represents insights</returns>
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
        public static DiagnosticData AddInsight(this Response response, Insight insight, string title = null, string description = null)
        {
            if (insight == null) return null;

            return AddInsights(response, new List<Insight>() { insight }, title, description);
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
    }
}
