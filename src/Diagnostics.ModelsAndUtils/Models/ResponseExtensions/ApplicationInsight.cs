using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Diagnostics.ModelsAndUtils.Utilities;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class BladeInfo
    {
        public BladeInfo(string bladeName, string description)
        {
            this.BladeName = bladeName;
            this.Description = description;
        }

        /// <summary>
        /// Application insights blade name
        /// </summary>
        public string BladeName { get; set; }

        /// <summary>
        /// Description of the application insights blade
        /// </summary>
        public string Description { get; set; }
    }

    public class AppInsightsOperationContext
    {
        /// <summary>
        /// This defines the application insights analysis metadata with query that will be executed and rendered in external portal.
        /// </summary>
        /// <param name="title">Application insights analysis title</param>
        /// <param name="description">Application insights analysis description</param>
        /// <param name="query">The query string to run against application insight instance</param>
        /// <param name="portalBladeInfo">The blade to suggest to customer for this analysis</param>
        /// <param name="renderingProperties">The rendering properties to render the application insight query result</param>
        /// <returns>AppInsightsOperationContext Object</returns>
        /// <example>
        public AppInsightsOperationContext(string title, string description, string query, BladeInfo portalBladeInfo, Rendering renderingProperties)
        {
            this.Title = title;
            this.Description = description;
            this.Query = query;
            this.PortalBladeInfo = portalBladeInfo;
            this.RenderingProperties = renderingProperties;
        }

        public AppInsightsOperationContext(string title, string description, string query, BladeInfo portalBladeInfo, Rendering renderingProperties, DataTable dataTable)
        {
            this.Title = title;
            this.Description = description;
            this.Query = query;
            this.PortalBladeInfo = portalBladeInfo;
            this.RenderingProperties = renderingProperties;
            if (dataTable != null && dataTable.Rows.Count > 0)
            {
                this.DataTable = dataTable.ToDataTableResponseObject();
            }

        }
        /// <summary>
        /// Application Insights rendering title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Application Insights rendering description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Application Insights query
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Application insight blade to be opened
        /// </summary>
        public BladeInfo PortalBladeInfo { get; set; }

        /// <summary>
        /// Rendering type
        /// </summary>
        public Rendering RenderingProperties { get; set; }

        public DataTableResponseObject DataTable { get; private set; }
    }
    public static class ApplicationInsight
    {
        /// <summary>
        /// Adds application Insight analysis metadata list.
        /// </summary>
        /// <param name="res">Response object for extension method</param>
        /// <param name="applicationInsightsList">A list of AppInsightsOperationContext object, each object defines the application insight analysis metadata.</param>
        /// <returns>DiagnosticData Object</returns>
        /// <example>
        /// This sample shows how to add application insight analysis <see cref="AddApplicationInsightsViewList"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///      BladeInfo appInsightsFailuresBlade = new BladeInfo("FailuresCuratedFrameBlade", "Open Application Insight Failures Blade");
        ///     
        ///     var appInsightsOperationContextException = new AppInsightsOperationContext(
        ///         "Application Exceptions that occured during this time period",
        ///         "",
        ///         @"exceptions 
        ///         | where timestamp > ago(24h) 
        ///         | where client_Type == 'PC'
        ///         | summarize Count = count() by outerMessage, problemId, type
        ///         | top 5 by Count desc
        ///         | project Message = outerMessage, Exception = problemId, Count",
        ///        appInsightsFailuresBlade,
        ///        new Rendering(RenderingType.Table));
        ///        
        ///     var appInsightslist = new List<AppInsightsOperationContext> { appInsightsOperationContextException};
        ///     res.AddApplicationInsightsViewList(appInsightslist);
        ///     
        ///     return res;
        /// }
        /// </code>
        /// </example>

        public static DiagnosticData AddApplicationInsightsViewList(this Response response, List<AppInsightsOperationContext> applicationInsightsList)
        {
            if (applicationInsightsList == null || !applicationInsightsList.Any())
                throw new ArgumentNullException("Parameter applicationInsightsList is null or contains no elements.");

            var table = new DataTable();
            table.Columns.Add("Title", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Query", typeof(string));
            table.Columns.Add("PortalBladeInfo", typeof(BladeInfo));
            table.Columns.Add("RenderingProperties", typeof(Rendering));
            table.Columns.Add("DataTable", typeof(DataTableResponseObject));

            foreach (AppInsightsOperationContext context in applicationInsightsList)
            {
                table.Rows.Add(
                    context.Title,
                    context.Description,
                    context.Query,
                    context.PortalBladeInfo,
                    context.RenderingProperties,
                    context.DataTable
                    );
            }

            var diagnosticData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.AppInsight)
            };

            response.Dataset.Add(diagnosticData);
            return diagnosticData;
        }
    }
}
