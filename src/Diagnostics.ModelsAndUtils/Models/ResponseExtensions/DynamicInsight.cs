using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    /// <summary>
    /// Class representing Dynamic Insight
    /// </summary>
    public class DynamicInsight
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
        /// Description to accompany data in body
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// whether Insight is expanded
        /// </summary>
        public bool Expanded { get; set; }

        /// <summary>
        /// Diagnostic data to render in body
        /// </summary>
        public DiagnosticData InnerDiagnosticData { get; set; }

        /// <summary>
        /// Creates an instance of Dynamic Insight class.
        /// </summary>
        /// <param name="status">Enum reprensenting insight level.</param>
        /// <param name="message">Insight Message.</param>
        public DynamicInsight(InsightStatus status, string message)
        {
            this.Status = status;
            this.Message = message ?? string.Empty;
            this.Expanded = true;
            this.Description = string.Empty;
            this.InnerDiagnosticData = new DiagnosticData();
        }

        /// <summary>
        /// Creates an instance of Dynamic Insight class.
        /// </summary>
        /// <param name="status">Enum reprensenting insight level.</param>
        /// <param name="message">Insight Message.</param>
        /// <param name="data">Diagnostic Data to be displayed in the insight</param>
        /// <param name="expanded">Whether insight is expanded to begin with</param>
        /// <param name="description">Description to accompany data in insight</param>
        public DynamicInsight(InsightStatus status, string message, DiagnosticData data, bool expanded = true, string description = "") : this(status, message)
        {
            this.Expanded = expanded;
            this.Description = description;
            this.InnerDiagnosticData = data;
        }
    }

    public static partial class ResponseInsightsExtension
    {

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
        ///     var diagnosticData = new DiagnosticData {
        ///         Table = table //This is where you would execute kusto query
        ///         RenderingProperties = new TimeSeriesRendering()
        ///     }
        /// 
        ///     Insight insight = new DynamicInsight(InsightStatus.Critical, "This insight will expand to show a graph", diagnosticData);
        ///     
        ///     res.AddDynamicInsight(insight);
        /// }
        /// </code>
        /// </example>
        public static DiagnosticData AddDynamicInsight(this Response response, DynamicInsight insight)
        {
            if (insight == null) return null;

            var diagnosticData = new DiagnosticData()
            {
                Table = insight.InnerDiagnosticData.Table,
                RenderingProperties = new DynamicInsightRendering()
                {
                    Title = insight.Message,
                    Description = insight.Description,
                    InnerRendering = insight.InnerDiagnosticData.RenderingProperties,
                    Status = insight.Status,
                    Expanded = insight.Expanded
                }
            };

            response.Dataset.Add(diagnosticData);

            return diagnosticData;
        }
    }
}
