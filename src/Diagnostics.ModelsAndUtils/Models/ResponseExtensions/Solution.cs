using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public enum SolutionActionType
    {
        Internal,
        External
    }

    public interface IHttpResponse
    {
        string Body { get; set; }
        string ResponseCode { get; set; }
        int StatusCode { get; set; }
    }

    public class Solution
    {
        public string Title;
        public IEnumerable<string> Descriptions;
        // In case confirmation is needed for "instant" actions like restart, which could down the service
        public bool RequiresConfirmation;
        public SolutionActionType ActionType;

        // Option 1: Call an azure operation (Ex. Restart Service)
        // Not sure what this would require, maybe it uses http call? 
        // TODO: Research ARM interface

        // Option 2: Navigate to an Azure blade
        // Need resource group, resource name, blade name
        // Internally wire these to make a url the user can click
        public string ResourceGroup;
        public string ResourceName;
        public string BladeName;

        // Complete action via API - uri, body, verb, etc.
        public Uri ActionUri;
        public string ContentType;
        public string Content;
        public IHttpResponse Response;

        public Solution(string title, IEnumerable<string> descriptions, bool confirm = false)
        {
            Title = title;
            Descriptions = descriptions;
            RequiresConfirmation = confirm;
        }
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
                nameof(Solution.RequiresConfirmation),
                nameof(Solution.ResourceGroup),
                nameof(Solution.ResourceName),
                nameof(Solution.BladeName) })
            {
                table.Columns.Add(new DataColumn(label, typeof(string)));
            }

            table.Rows.Add(new object[]
            {
                solution.Title,
                // Strange antipattern
                JsonConvert.SerializeObject(solution.Descriptions),
                solution.RequiresConfirmation,
                solution.ResourceGroup,
                solution.ResourceName,
                solution.BladeName
            });

            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Solution)
            };

            response.Dataset.Add(diagData);
            return diagData;
        }

        public static DiagnosticData AddSolution(this Response response, Insight forInsight, Solution solution)
        {
            var table = new DataTable();
            foreach (var label in new List<string>() {
                nameof(Solution.Title),
                nameof(Solution.Descriptions),
                nameof(Solution.RequiresConfirmation),
                nameof(Solution.ResourceGroup),
                nameof(Solution.ResourceName),
                nameof(Solution.BladeName)
            })
            {
                table.Columns.Add(new DataColumn(label, typeof(string)));
            }

            table.Rows.Add(new object[]
            {
                solution.Title,
                // Strange antipattern
                JsonConvert.SerializeObject(solution.Descriptions),
                solution.RequiresConfirmation,
                solution.ResourceGroup,
                solution.ResourceName,
                solution.BladeName
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
