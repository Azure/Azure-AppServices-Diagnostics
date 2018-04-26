using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    /// <summary>
    /// Class representing Dynamic Insight
    /// </summary>
    public class DynamicInsight : InsightBase
    {
        public string Description { get; set; }

        public bool Expanded { get; set; }

        public DiagnosticData InnerDiagnosticData { get; set; }

        /// <summary>
        /// Creates an instance of Dynamic Insight class.
        /// </summary>
        /// <param name="status">Enum reprensenting insight level.</param>
        /// <param name="message">Insight Message.</param>
        public DynamicInsight(InsightStatus status, string message) : base(status, message)
        {
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
        public DynamicInsight(InsightStatus status, string message, DiagnosticData data, bool expanded = true, string description = "") : base(status, message)
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
        ///     Insight insight = new Insight(InsightStatus.Critical, "insight1 title");
        ///     
        ///     res.AddInsight(insight);
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
