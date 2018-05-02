using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public static class ResponseMarkdownExtension
    {
        public static DiagnosticData AddMarkdownView(this Response response, string markdown, string title = null)
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("Markdown", typeof(string)));
            table.Rows.Add(new object[] { markdown });
            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Markdown)
                {
                    Title = title
                }
            };

            response.Dataset.Add(diagData);
            return diagData;
        }
    }
}
