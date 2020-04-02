using System;

namespace Diagnostics.ModelsAndUtils.Models.ChangeAnalysis
{
    /// <summary>
    /// This model is used for making request to changeset endpoint.
    /// </summary>
    public class ChangeSetsRequest
    {
        /// <summary>
        /// ARM resource ID.
        /// </summary>
        public string ResourceId;

        /// <summary>
        /// Start time of the changeset requested.
        /// </summary>
        public DateTime StartTime;

        /// <summary>
        /// End time of the changeset requested.
        /// </summary>
        public DateTime EndTime;
    }
}
