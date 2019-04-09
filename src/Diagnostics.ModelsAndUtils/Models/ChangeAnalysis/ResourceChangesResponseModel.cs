using System;
using Newtonsoft.Json;
using System.Collections.Generic;
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
        /// Category of the change belonging to <see cref="ChangeCategory"/>.
        /// </summary>
        public ChangeCategory Category;

        /// <summary>
        /// Level of the change belonging to <see cref="ChangeLevel"/>.
        /// </summary>
        public ChangeLevel Level;

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
        [JsonConverter(typeof(SingleOrArrayConverter<object>))]
        public object OldValue;

        /// <summary>
        /// New value.
        /// </summary>
        [JsonConverter(typeof(SingleOrArrayConverter<object>))]
        public object NewValue;

        /// <summary>
        /// Initiated By. It can be email address or Guid.
        /// </summary>
        public string InitiatedBy;

        /// <summary>
        /// Json path obtained from definition.
        /// </summary>
        public string JsonPath;
    }

    public enum ChangeCategory
    {
        Uncategorized = 0,
        Network,
        HostEnv,
        AppConfig,
        Envar,
        Others
    }

    public enum ChangeLevel
    {
        Noise = 0,
        Normal,
        Important
    }
}
