using System.Collections.Generic;
using System.Text.Json.Serialization;
using Diagnostics.ModelsAndUtils.ScriptUtilities;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class Solution
    {
        /// <summary>
        /// Name of the Solution.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Solution title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// A description which will be rendered as Markdown.
        /// </summary>
        public string DescriptionMarkdown { get; set; }

        /// <summary>
        /// Denotes which action will be performed, such as calling an ARM API or navigating to a Portal Blade.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ActionType Action { get; set; }

        /// <summary>
        /// Options pertaining to <see cref="ActionType.ArmApi"/>.
        /// This is ignored if <see cref="Action"/> is not <see cref="ActionType.ArmApi"/>.
        /// </summary>
        public ArmApiOptions ApiOptions { get; set; }

        /// <summary>
        /// Options pertaining to <see cref="ActionType.GoToBlade"/>.
        /// This is ignored if <see cref="Action"/> is not <see cref="ActionType.GoToBlade"/>.
        ///
        /// Configuring BladeOptions:
        /// Navigate to the the blade you would like to configure in Azure Portal.
        /// Press ctrl+alt+d to open the Ibiza debugging context. Open the debugger in your browser as well.
        /// Click on the link with the blade name in the yellow popup debug window.
        /// A console event will show the BladeName and BladeInputs passed to Ibiza for the blade.
        /// Copy these into the DetailBlade and DetailBladeInputs of the GoToBladeOptions object.
        /// </summary>
        public GoToBladeOptions BladeOptions { get; set; }

        /// <summary>
        /// Options pertaining to <see cref="ActionType.OpenTab"/>.
        /// This is ignored if <see cref="Action"/> is not <see cref="ActionType.OpenTab"/>.
        /// </summary>
        public OpenTabOptions TabOptions { get; set; }

        /// <summary>
        /// Free-form JSON-serializable options that will override <see cref="ApiOptions"/>,
        /// <see cref="BladeOptions"/>, or <see cref="TabOptions"/> for the purpose of compatibility.
        /// Prefer the aforementioned first-class option objects.
        /// </summary>
        public Dictionary<string, object> OverrideOptions { get; set; }

        /// <summary>
        /// If the solution requires confirmation, it will signify a potentially dangerous action in the UI.
        /// </summary>
        public bool RequiresConfirmation { get; set; }

        /// <summary>
        /// The URI of the target resource on which an action will be performed.
        /// </summary>
        public string ResourceUri { get; set; }

        /// <summary>
        /// Instructions that will be sent to the customer by support staff. Rendered as Markdown.
        /// Any instance of "{detectorLink}" will be replaced with a link to the detector.
        /// If "{detectorLink}" is not present in the string, the detector link will be appended to the
        /// end of the markdown.
        /// </summary>
        public string InternalMarkdown { get; set; }

        public SolutionTypeTag TypeTag { get; set; }

        /// <summary>
        /// This is set automatically.
        /// Reads <see cref="OperationContext"/> to determine if an internal view should be rendered.
        /// </summary>
        public bool IsInternal { get; set; } = false;

        /// <summary>
        /// This is set automatically.
        /// Used to create a deep link back to the Solution's detector in case the customer is receiving
        /// Solution information from support staff.
        /// </summary>
        public string DetectorId { get; set; }

        /// <summary>
        /// Set the ResourceUri and optionally set the ActionOptions for the Solution to use.
        /// </summary>
        // TODO: actionOptions has to merge with existing options in case detector author adds to solution author's options
        public Solution Using(string resourceUri, Dictionary<string, object> actionOptions = null)
        {
            ResourceUri = resourceUri;

            if (actionOptions != null)
            {
                OverrideOptions = actionOptions;
            }

            return this;
        }

        /// <summary>
        /// Set the ResourceUri and optionally set the ActionOptions for the Solution to use.
        /// </summary>
        public Solution Using(OperationContext context, Dictionary<string, object> actionOptions = null)
        {
            return Using(context.Resource.ResourceUri, actionOptions);
        }
    }

    public class SolutionButtonOption
    {
        public string Label { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SolutionButtonType Type { get; set; } = SolutionButtonType.Button;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SolutionButtonPosition Position { get; set; } = SolutionButtonPosition.Bottom;

        public SolutionButtonOption(string label, SolutionButtonType type = SolutionButtonType.Button, SolutionButtonPosition position = SolutionButtonPosition.Bottom)
        {
            this.Label = label;
            this.Type = type;
            this.Position = position;
        }
    }

    public enum SolutionButtonType
    {
        Button,
        Link,
    }

    public enum SolutionButtonPosition
    {
        Bottom = 0,
        NextToTitle = 1,
    }
}
