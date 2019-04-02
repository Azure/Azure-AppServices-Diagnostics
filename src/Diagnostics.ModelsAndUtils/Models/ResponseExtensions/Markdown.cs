using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public static class ResponseMarkdownExtension
    {
        /// <summary>
        /// Adds markdown section to the response
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="markdown">String that will be translated to markdown in UI</param>
        /// <param name="title">Title of markdown section</param>
        /// <example> 
        /// This sample shows how to use <see cref="AddMarkdownView"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     var markdown = @"
        ///     ## This is header
        ///     ";
        ///     
        ///     res.AddMarkdownView(markdown, "Title of markdown section");
        /// }
        /// </code>
        /// </example>
        public static DiagnosticData AddMarkdownView(this Response response, string markdown, string title = null)
        {
            return response.AddMarkdownView(markdown, false, title);
        }

        /// <summary>
        /// Adds markdown section to the response
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="isContainerNeeded">If true, will keep the container of markdown view</param>
        /// <param name="markdown">String that will be translated to markdown in UI</param>
        /// <param name="title">Title of markdown section</param>
        /// <example> 
        /// This sample shows how to use <see cref="AddMarkdownView"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     var markdown = @"
        ///     ## This is header
        ///     ";
        ///     
        ///     res.AddMarkdownView(markdown, "Title of markdown section");
        /// }
        /// </code>
        /// </example>

        public static DiagnosticData AddMarkdownView(this Response response, bool isContainerNeeded, string markdown, string title = null)
        {
            return response.AddMarkdownView(markdown, false, title, isContainerNeeded);
        }

        /// <summary>
        /// Adds markdown section to the response
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="markdown">String that will be translated to markdown in UI</param>
        /// <param name="title">Title of markdown section</param>
        /// <param name="enableEmailButtons">If true, will display copy and open in email button in Applens</param>
        /// <param name="isContainerNeeded">If true, will keep the container of markdown view</param>
        /// <example> 
        /// This sample shows how to use <see cref="AddMarkdownView"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     var markdown = @"
        ///     ## This is header
        ///     ";
        ///     
        ///     res.AddMarkdownView(markdown, "Title of markdown section", enableEmailButtons = true);
        /// }
        /// </code>
        /// </example>
        public static DiagnosticData AddMarkdownView(this Response response, string markdown, bool enableEmailButtons, string title = null, bool isContainerNeeded = true)
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("Markdown", typeof(string)));
            table.Rows.Add(new object[] { markdown });
            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new MarkdownRendering()
                {
                    EnableEmailButtons = enableEmailButtons,
                    Title = title,
                    IsContainerNeeded = isContainerNeeded
                }
            };

            response.Dataset.Add(diagData);
            return diagData;
        }
    }
}
