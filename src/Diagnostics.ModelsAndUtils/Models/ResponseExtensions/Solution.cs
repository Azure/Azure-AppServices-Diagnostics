using System.Collections.Generic;
using System.Text;
using Diagnostics.ModelsAndUtils.Utilities;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class Solution
    {
        /// <summary>
        /// Solution title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Descriptions which will be viewed as Markdown.
        /// </summary>
        public IEnumerable<string> Descriptions { get; set; }

        /// <summary>
        /// Name of the action for display customization purposes.
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// If the solution requires confirmation, it will signify a potentially dangerous action in the UI.
        /// </summary>
        public bool RequiresConfirmation { get; set; }

        /// <summary>
        /// The URI of the target resource on which an action will be performed.
        /// </summary>
        public string ResourceUri { get; set; }

        /// <summary>
        /// Reads <see cref="OperationContext"/> to determine if an internal view should be rendered.
        /// </summary>
        public bool IsInternal { get; set; }

        /// <summary>
        /// Instructions that will be sent to the customer by support staff.
        /// </summary>
        public string InternalInstructions { get; set; }

        /// <summary>
        /// Denotes which action will be performed, such as a specific API call or navigation link.
        /// </summary>
        public ActionType Action { get; set; }

        /// <summary>
        /// Free-form JSON-serializable arguments that can be sent as the body of the action request if a request
        /// body is applicable.
        /// </summary>
        public Dictionary<string, object> ActionArgs { get; set; }

        /// <summary>
        /// Creates a Solution component. Prefer a pre-defined Solution component such as Solution.Restart.
        /// </summary>
        /// <param name="title">Solution title.</param>
        /// <param name="resourceUri">The URI of the target resource on which an action will be performed.</param>
        /// <param name="isInternal">Reads <see cref="OperationContext"/> to determine if an internal view should be rendered.</param>
        /// <param name="action">Denotes which action will be performed, such as a specific API call or navigation link.</param>
        /// <param name="internalInstructions">Instructions that will be sent to the customer by support staff.</param>
        /// <param name="descriptions">Descriptions which will be viewed as Markdown.</param>
        /// <param name="actionArgs">Free-form JSON-serializable arguments that can be sent as the body of the
        ///     action request if a request body is applicable.</param>
        /// <param name="confirm">If the solution requires confirmation, it will signify a potentially dangerous action in the UI.</param>
        /// <param name="actionName">Name of the action for display customization purposes.</param>
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

        /// <summary>
        /// Creates a Solution component. Prefer a pre-defined Solution component such as Solution.Restart.
        /// </summary>
        /// <param name="title">Solution title.</param>
        /// <param name="resourceUri">The URI of the target resource on which an action will be performed.</param>
        /// <param name="context">Determines if an internal view should be rendered.</param>
        /// <param name="action">Denotes which action will be performed, such as a specific API call or navigation link.</param>
        /// <param name="internalInstructions">Instructions that will be sent to the customer by support staff.</param>
        /// <param name="descriptions">Descriptions which will be viewed as Markdown.</param>
        /// <param name="actionArgs">Free-form JSON-serializable arguments that can be sent as the body of the
        ///     action request if a request body is applicable.</param>
        /// <param name="confirm">If the solution requires confirmation, it will signify a potentially dangerous action in the UI.</param>
        /// <param name="actionName">Name of the action for display customization purposes.</param>
        public Solution(string title, string resourceUri, OperationContext context, ActionType action,
            string internalInstructions, IEnumerable<string> descriptions = null,
            Dictionary<string, object> actionArgs = null, bool confirm = false, string actionName = "") :
            this(title, resourceUri, context.IsInternalCall, action, internalInstructions, descriptions, actionArgs,
                confirm)
        { }

        // TODO: This should pass any remaining arguments to the constructor
        // TODO: isInternal and detectorId should be integrated in backend, not passed here
        /// <summary>
        /// Creates a pre-defined Solution component capable of restarting an App instance
        /// </summary>
        public static Solution Restart(string resourceUri, bool isInternal, string detectorId)
        {
            var resourceLink = UriUtilities.BuildDetectorLink(resourceUri, detectorId);
            var instructions = $"[Go To Detector]({resourceLink})\n\n" +
                $"{SolutionConstants.RestartInstructions}";

            return new Solution("Restart Site", resourceUri, isInternal, ActionType.RestartSite, instructions,
                new string[] { SolutionConstants.AppRestartDescription }, confirm: true, actionName: "Restart App");
        }

        /// <summary>
        /// Creates a pre-defined Solution component capable of restarting an App instance
        /// </summary>
        public static Solution Restart(string resourceUri, OperationContext context, string detectorId)
        {
            return Restart(resourceUri, context.IsInternalCall, detectorId);
        }

        // TODO: isInternal and detectorId should be integrated in backend, not passed here
        /// <summary>
        /// Creates a pre-defined Solution component capable of updating the App Settings of a resource
        /// </summary>
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

        /// <summary>
        /// Creates a pre-defined Solution component capable of updating the App Settings of a resource
        /// </summary>
        public static Solution UpdateAppSettings(string resourceUri, OperationContext context, string detectorId,
            Dictionary<string, object> actionArgs)
        {
            return UpdateAppSettings(resourceUri, context.IsInternalCall, detectorId, actionArgs);
        }
    }

    /// <summary>
    /// Actions that a Solution can perform, such as ARM API requests.
    /// </summary>
    public enum ActionType
    {
        // ARM Actions
        RestartSite,
        UpdateSiteAppSettings,
        KillW3wpOnInstance
    }
}
