using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class Solution
    {
        public string Title { get; set; }
        public IEnumerable<string> Descriptions { get; set; }
        public string ResourceUri { get; set; }
        public bool RequiresConfirmation { get; set; }

        public Solution(string title, IEnumerable<string> descriptions, string resourceUri, bool confirm = false)
        {
            Title = title;
            Descriptions = descriptions;
            ResourceUri = resourceUri;
            RequiresConfirmation = confirm;
        }

        //TODO: Another overload taking ResourceGroup and SiteName to build resourceUri would also be useful
    }

    public static class ResponseSolutionExtension
    {
        // TODO: This workflow can be made generic, dynamically add new data types to response with a helper method
        public static DiagnosticData AddSolution(this Response response, Solution solution)
        {
            var table = new DataTable();
            foreach(var label in new List<string>() {
                nameof(Solution.Title),
                nameof(Solution.Descriptions),
                nameof(Solution.ResourceUri),
                nameof(Solution.RequiresConfirmation)
            })
            {
                table.Columns.Add(new DataColumn(label, typeof(string)));
            }

            table.Rows.Add(new object[]
            {
                solution.Title,
                // Strange antipattern
                JsonConvert.SerializeObject(solution.Descriptions),
                solution.ResourceUri,
                solution.RequiresConfirmation.ToString()
            });

            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Solution)
            };

            response.Dataset.Add(diagData);
            return diagData;
        }
    }
}
