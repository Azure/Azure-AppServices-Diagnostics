using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public enum ActionType
    {
        // ARM Actions
        RestartSite,
        UpdateSiteAppSettings,
        KillW3wpOnInstance
    }
    
    public class Solution
    {
        public string Title { get; set; }
        public IEnumerable<string> Descriptions { get; set; }
        public string ResourceUri { get; set; }
        public bool RequiresConfirmation { get; set; }
        public ActionType Action { get; set; }
        public Dictionary<string, object> ActionArgs { get; set; }

        // TODO: Another overload taking ResourceGroup and SiteName to build resourceUri would also be useful
        public Solution(string title, string resourceUri, ActionType action, IEnumerable<string> descriptions = null, 
            Dictionary<string, object> actionArgs = null, bool confirm = false)
        {
            Title = title;
            ResourceUri = resourceUri;
            Action = action;
            Descriptions = descriptions;
            ActionArgs = actionArgs;
            RequiresConfirmation = confirm;
        }

        // TODO: This should pass any remaining arguments to the constructor
        public static Solution Restart(string resourceUri)
        {
            return new Solution("Restart Site", resourceUri, ActionType.RestartSite,
                new string[] { SolutionConstants.RestartMarkdown }, confirm: true);
        }

        public static Solution UpdateAppSettings(string resourceUri, Dictionary<string, object> actionArgs)
        {
            var markdownBuilder = new StringBuilder();
            markdownBuilder.AppendLine("Apply the following settings changes:");
            markdownBuilder.AppendLine();
            markdownBuilder.Append(DictionaryToMarkdownList(actionArgs));

            return new Solution("Update App Settings", resourceUri, ActionType.UpdateSiteAppSettings, 
                new string[] { markdownBuilder.ToString() }, actionArgs);
        }

        private static string DictionaryToMarkdownList(Dictionary<string, object> input)
        {
            var markdownBuilder = new StringBuilder();

            foreach (var kvp in input)
            {
                var value = "";

                if (kvp.Value.GetType() != typeof(string))
                {
                    value = JsonConvert.SerializeObject(kvp.Value);
                }
                else
                {
                    value = kvp.Value.ToString();
                }

                if (value.Length > 17)
                {
                    value = value.Truncate(17);
                    value = $"{value}...";
                }

                markdownBuilder.AppendLine($" - {kvp.Key}: {value}");
            }

            return markdownBuilder.ToString();
        }
    }

    public static class SolutionConstants
    {
        public static readonly string RestartMarkdown = @"Restarting the site may cause application downtime";
    }

    public static class StringExtensions
    {        
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
