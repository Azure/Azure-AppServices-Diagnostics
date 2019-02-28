using Diagnostics.ModelsAndUtils.Utilities;
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
        public string ActionName { get; set; }
        public bool RequiresConfirmation { get; set; }
        public string ResourceUri { get; set; }
        public bool IsInternal { get; set; }
        public string InternalInstructions { get; set; }
        public ActionType Action { get; set; }
        public Dictionary<string, object> ActionArgs { get; set; }

        public Solution(string title, string resourceUri, bool isInternal, ActionType action, 
            string internalInstructions, IEnumerable<string> descriptions = null, 
            Dictionary<string, object> actionArgs = null, bool confirm = false, string actionName = "")
        {
            Title = title;
            ResourceUri = resourceUri;
            Action = action;
            IsInternal = isInternal;
            InternalInstructions = internalInstructions;
            Descriptions = descriptions;
            ActionArgs = actionArgs;
            RequiresConfirmation = confirm;
            ActionName = actionName != string.Empty ? actionName : title;
        }

        public Solution(string title, string resourceUri, OperationContext context, ActionType action,
            string internalInstructions, IEnumerable<string> descriptions = null,
            Dictionary<string, object> actionArgs = null, bool confirm = false, string actionName = "") :
            this(title, resourceUri, context.IsInternalCall, action, internalInstructions,
                descriptions, actionArgs, confirm)
        { }

        // TODO: This should pass any remaining arguments to the constructor
        // TODO: isInternal and detectorId should be integrated in backend, not passed here
        public static Solution Restart(string resourceUri, bool isInternal, string detectorId)
        {
            var resourceLink = UriUtilities.BuildDetectorLink(resourceUri, detectorId);
            var instructions = $"[Go To Detector]({resourceLink})\n\n" +
                $"{SolutionConstants.RestartInstructions}";

            return new Solution("Restart Site", resourceUri, isInternal, ActionType.RestartSite, instructions,
                new string[] { SolutionConstants.AppRestartDescription }, 
                confirm: true, actionName: "Restart App");
        }

        public static Solution Restart(string resourceUri, OperationContext context, string detectorId)
        {
            return Restart(resourceUri, context.IsInternalCall, detectorId);
        }

        // TODO: isInternal and detectorId should be integrated in backend, not passed here
        public static Solution UpdateAppSettings(string resourceUri, bool isInternal, string detectorId, 
            Dictionary<string, object> actionArgs)
        {
            var descriptionBuilder = new StringBuilder();
            descriptionBuilder.AppendLine("Apply the following settings changes:");
            descriptionBuilder.AppendLine();
            descriptionBuilder.Append(actionArgs.ToMarkdownList());

            var resourceLink = UriUtilities.BuildDetectorLink(resourceUri, detectorId);

            var instructions = $"[Go To Detector]({resourceLink})\n\n" +
                $"{SolutionConstants.UpdateSettingsInstructions}\n{actionArgs.ToMarkdownList(new string(' ', 3))}";

            return new Solution("Update App Settings", resourceUri, isInternal, ActionType.UpdateSiteAppSettings,
                instructions, new string[] { descriptionBuilder.ToString() }, actionArgs);
        }

        public static Solution UpdateAppSettings(string resourceUri, OperationContext context, string detectorId, 
            Dictionary<string, object> actionArgs)
        {
            return UpdateAppSettings(resourceUri, context.IsInternalCall, detectorId, actionArgs);
        }
    }

    public enum ActionType
    {
        // ARM Actions
        RestartSite,
        UpdateSiteAppSettings,
        KillW3wpOnInstance
    }
}
