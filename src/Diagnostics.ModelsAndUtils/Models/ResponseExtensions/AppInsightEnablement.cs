using System;
using System.Data;
using System.Text.RegularExpressions;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public static class ResponseAppInsightEnablementExtension
    {
        public static DiagnosticData AddAppInsightsEnablement(this Response response, string resourceUri)
        {

            if (string.IsNullOrWhiteSpace(resourceUri))
            {
                throw new ArgumentNullException(nameof(resourceUri));
            }

            resourceUri = resourceUri.ToLower();
            if (!ValidateResourceUri(resourceUri))
            {
                throw new ArgumentException("resourceUri not in the correct format");
            }

            var table = new DataTable();
            table.Columns.Add(new DataColumn("ResourceUri", typeof(string)));
            table.Rows.Add(new object[] { resourceUri });

            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.AppInsightEnablement)
            };

            response.Dataset.Add(diagData);
            return diagData;
        }

        private static bool ValidateResourceUri(string resourceUri)
        {
            Regex resourceRegEx = new Regex("/subscriptions/(.*)/resourcegroups/(.*)/providers/(.*)/(.*)/(.*)");
            Match match = resourceRegEx.Match(resourceUri);
            return match.Success;
        }
    }
}
