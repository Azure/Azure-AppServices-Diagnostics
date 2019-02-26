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
        // Really don't like this, would like to pull isInternal in the context without adding to the constructor
        public bool IsInternal { get; set; }
        public string InternalInstructions { get; set; }
        public bool RequiresConfirmation { get; set; }
        public ActionType Action { get; set; }
        public Dictionary<string, object> ActionArgs { get; set; }

        // TODO: Another overload taking ResourceGroup and SiteName to build resourceUri would also be useful
        public Solution(string title, string resourceUri, ActionType action, bool isInternal,
            string internalInstructions, IEnumerable<string> descriptions = null, 
            Dictionary<string, object> actionArgs = null, bool confirm = false)
        {
            Title = title;
            ResourceUri = resourceUri;
            Action = action;
            IsInternal = isInternal;
            InternalInstructions = internalInstructions;
            Descriptions = descriptions;
            ActionArgs = actionArgs;
            RequiresConfirmation = confirm;
        }

        // TODO: This should pass any remaining arguments to the constructor
        // TODO: isInternal and detectorId should be integrated in backend, not passed here
        public static Solution Restart(string resourceUri, bool isInternal, string detectorId)
        {
            var resourceLink = BuildDetectorLink(resourceUri, detectorId);
            var instructions = $"{new string(' ', 2)}[Go To Resource]({resourceLink})\n\n{SolutionConstants.RestartInstructions}";

            return new Solution("Restart Site", resourceUri, ActionType.RestartSite, isInternal, instructions,
                new string[] { SolutionConstants.RestartDescription }, confirm: true);
        }

        // TODO: isInternal and detectorId should be integrated in backend, not passed here
        public static Solution UpdateAppSettings(string resourceUri, bool isInternal, string detectorId, Dictionary<string, object> actionArgs)
        {
            var descriptionBuilder = new StringBuilder();
            descriptionBuilder.AppendLine("Apply the following settings changes:");
            descriptionBuilder.AppendLine();
            descriptionBuilder.Append(DictionaryToMarkdownList(actionArgs));

            var resourceLink = BuildDetectorLink(resourceUri, detectorId);

            var instructions = $"{new string(' ', 2)}[Go To Settings]({resourceLink})\n\n{SolutionConstants.UpdateSettingsInstructions}" +
                $"\n\n{DictionaryToMarkdownList(actionArgs, new string(' ', 5))}";

            return new Solution("Update App Settings", resourceUri, ActionType.UpdateSiteAppSettings, isInternal,
                instructions, new string[] { descriptionBuilder.ToString() }, actionArgs);
        }

        private static string DictionaryToMarkdownList(Dictionary<string, object> input, string indent = " ")
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

                markdownBuilder.AppendLine($"{indent}- {kvp.Key}: {value}");
            }

            return markdownBuilder.ToString();
        }

        // TODO: Move to utility
        private static string BuildDetectorLink(string resourceUri, string detectorId)
        {
            return "https://portal.azure.com/?websitesextension_ext=asd.featurePath=" +
                $"diagnostics/{detectorId}#resource/{resourceUri.Trim('/')}/troubleshoot";
        }
    }

    public static class SolutionConstants
    {
        public static readonly string DetectorIDReplaceMe = "appcrashes";
        public static readonly string RestartDescription = "Restarting the site may cause application downtime";
        public static readonly string RestartInstructions = @"
    1. Navigate to the resource in Azure Portal
    2. Press `Restart` to invoke a site restart";
        public static readonly string UpdateSettingsInstructions = @"
    1. Navigate to the resource in Azure Portal
    2. Navigate to the `Application Settings` tab
    3. Enter the following settings under the `Application Settings` section:";
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
