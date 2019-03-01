using System;
using System.Collections.Generic;
using System.Text;
using Diagnostics.ModelsAndUtils.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class Solution
    {
        /// <summary>
        /// Solution title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Descriptions which will be rendered as Markdown.
        /// </summary>
        public string Description { get; set; }

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
        public bool IsInternal { get; set; } = false;

        /// <summary>
        /// Instructions that will be sent to the customer by support staff. Rendered in Markdown.
        /// </summary>
        public string InternalInstructions { get; set; }

        /// <summary>
        /// A direct link to the detector that is added to the instructions for the customer.
        /// </summary>
        public Uri DetectorLink { get; set; }

        /// <summary>
        /// Denotes which action will be performed, such as a specific API call or navigation link.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ActionType Action { get; set; }

        /// <summary>
        /// Free-form JSON-serializable arguments that can be sent as the body of the action request if a request
        /// body is applicable.
        /// </summary>
        public Dictionary<string, object> ActionArgs { get; set; }

        /// <summary>
        /// Adds a pre-written <see cref="Description"/> to the Solution. Takes priority over Description values.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public SolutionText? PremadeDescription { get; set; }

        /// <summary>
        /// Adds pre-written <see cref="InternalInstructions"/> to the Solution. Takes priority over
        /// InternalInstructions values.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public SolutionText? PremadeInstructions { get; set; }

        /// <summary>
        /// Creates a Solution component. Prefer a pre-defined Solution component such as <see cref="Solution.Restart()"/>.
        /// </summary>
        /// <param name="title">Solution title.</param>
        /// <param name="resourceUri">The URI of the target resource on which an action will be performed.</param>
        /// <param name="isInternal">Reads <see cref="OperationContext"/> to determine if an internal view should be rendered.</param>
        /// <param name="action">Denotes which action will be performed, such as a specific API call or navigation link.</param>
        public Solution(string title, string resourceUri, ActionType action)
        {
            Title = title;
            ResourceUri = resourceUri;
            Action = action;
            ActionName = title;
        }

        /// <summary>
        /// Creates a Solution component. Prefer a pre-defined Solution component such as <see cref="Solution.Restart()"/>.
        /// </summary>
        /// <param name="title">Solution title.</param>
        /// <param name="resourceUri">The URI of the target resource on which an action will be performed.</param>
        /// <param name="context">Determines if an internal view should be rendered.</param>
        /// <param name="action">Denotes which action will be performed, such as a specific API call or navigation link.</param>
        public Solution(string title, string resourceUri, OperationContext context, ActionType action) :
            this(title, resourceUri, action)
        { }

        // TODO: This should pass any remaining arguments to the constructor
        /// <summary>
        /// Creates a pre-defined Solution component capable of restarting an App instance
        /// </summary>
        public static Solution Restart(string resourceUri)
        {
            return new Solution("Restart Site", resourceUri, ActionType.RestartSite)
            {
                RequiresConfirmation = true,
                ActionName = "Restart App",
                PremadeDescription = SolutionText.AppRestartDescription,
                PremadeInstructions = SolutionText.RestartInstructions
            };
        }

        /// <summary>
        /// Creates a pre-defined Solution component capable of updating the App Settings of a resource
        /// </summary>
        public static Solution UpdateAppSettings(string resourceUri, Dictionary<string, object> actionArgs)
        {
            return new Solution("Update App Settings", resourceUri, ActionType.UpdateSiteAppSettings)
            {
                ActionArgs = actionArgs,
                PremadeDescription = SolutionText.UpdateSettingsDescription,
                PremadeInstructions = SolutionText.UpdateSettingsInstructions
            };
        }
    }

    /// <summary>
    /// Actions that a Solution can perform, such as ARM API requests.
    /// </summary>
    public enum ActionType
    {
        RestartSite,
        UpdateSiteAppSettings,
        KillW3wpOnInstance
    }
}
