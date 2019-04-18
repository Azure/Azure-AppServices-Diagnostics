namespace Diagnostics.ModelsAndUtils.Models.ChangeAnalysis
{
    /// <summary>
    /// This model is used for querying changes related to a particular ChangeSetId
    /// </summary>
    public class ChangeRequest
    {
        /// <summary>
        /// ARM Resource Id.
        /// </summary>
        public string ResourceId;

        /// <summary>
        /// ChangeSetId used to query changes.
        /// </summary>
        public string ChangeSetId;
    }
}
