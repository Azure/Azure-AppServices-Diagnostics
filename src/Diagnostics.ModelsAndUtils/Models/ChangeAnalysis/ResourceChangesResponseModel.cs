using Newtonsoft.Json.Linq;

namespace Diagnostics.ModelsAndUtils.Models.ChangeAnalysis
{
    /// <summary>
    /// This model represents the changes data captured for a changeset in ARM Resource.
    /// </summary>
    public class ResourceChangesResponseModel
    {
        /// <summary>
        /// Timestamp of the change.
        /// </summary>
        public string TimeStamp;

        /// <summary>
        /// Category of the change. It can be uncategorized, network, hostenv, appconfig, envvar, others.
        /// </summary>
        public string Category;

        /// <summary>
        /// Level of the change. It can be noisy, normal, important.
        /// </summary>
        public string Level;

        /// <summary>
        /// Display name (property name).
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// Description of the change.
        /// </summary>
        public string Description;

        /// <summary>
        /// Old value.
        /// </summary>
        public JToken OldValue;

        /// <summary>
        /// New value.
        /// </summary>
        public JToken NewValue;

        /// <summary>
        /// Initiated By. It can be email address or Guid.
        /// </summary>
        public string InitiatedBy;

        /// <summary>
        /// Json path obtained from definition.
        /// </summary>
        public string JsonPath;
    }
}
