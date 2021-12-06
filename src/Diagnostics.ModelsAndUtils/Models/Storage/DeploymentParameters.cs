using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Diagnostics.ModelsAndUtils.Models.Storage
{
    /// <summary>
    /// Deployment Parameters used by client/service caller to deploy detectors.
    /// </summary>
    public class DeploymentParameters
    {
        /// <summary>
        /// Single commit id to deploy. Not applicable if <see cref="FromCommitId"/> and <see cref="ToCommitId"/> is given.
        /// </summary>
        [JsonPropertyName("CommitId")]
        public string CommitId { get; set; }

        /// <summary>
        /// Start commit to deploy. Not applicable if <see cref="CommitId"/> is given.
        /// </summary>
        [JsonPropertyName("FromCommitId")]
        public string FromCommitId { get; set; }

        /// <summary>
        /// End commit to deploy. Not applicable if <see cref="CommitId"/> is given.
        /// </summary>
        [JsonPropertyName("ToCommitId")]
        public string ToCommitId { get; set; }

        /// <summary>
        /// If provided, includes detectors modified after this date. Cannot be combined with <see cref="FromCommitId"/> and <see cref="ToCommitId"/>.
        /// </summary>
        [JsonPropertyName("StartDate")]
        public string StartDate { get; set; }

        /// <summary>
        /// If provided, includes detectors modified before this date. Cannot be combined with <see cref="FromCommitId"/> and <see cref="ToCommitId"/>.
        /// </summary>
        [JsonPropertyName("EndDate")]
        public string EndDate { get; set; }

        /// <summary>
        /// Resource type of the caller. eg. Microsoft.Web/sites.
        /// </summary>
        [JsonPropertyName("ResourceType")]
        public string ResourceType { get; set; }
    }

    /// <summary>
    /// Deployment response sent back to the caller;
    /// </summary>
    public class DeploymentResponse
    {
        /// <summary>
        /// List of detectors that got updated/added/edited;
        /// </summary>

        [JsonPropertyName("DeployedDetectors")]
        public List<string> DeployedDetectors { get; set; }

        /// <summary>
        /// List of detectors that failed deployment along with the reason of failure;
        /// </summary>

        [JsonPropertyName("FailedDetectors")]
        public Dictionary<string, string> FailedDetectors { get; set; }

        /// <summary>
        /// List of detectors that were marked for deletion;
        /// </summary>

        [JsonPropertyName("DeletedDetectors")]
        public List<string> DeletedDetectors { get; set; }

        /// <summary>
        /// Unique Guid to track the deployment;
        /// </summary>

        [JsonPropertyName("DeploymentGuid")]
        public string DeploymentGuid { get; set; }
    }
}
