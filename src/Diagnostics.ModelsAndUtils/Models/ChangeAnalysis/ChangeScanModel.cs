namespace Diagnostics.ModelsAndUtils.Models.ChangeAnalysis
{
    /// <summary>
    /// Model used to submit scan and check if any scans are running.
    /// </summary>
    public class ChangeScanModel
    {
        /// <summary>
        /// Scan operation id.
        /// </summary>
        public string OperationId;

        /// <summary>
        /// Scan state. It can be Submitted, InScan, Completed, Failed.
        /// </summary>
        public string State;

        /// <summary>
        /// Timestamp when the request was submitted.
        /// </summary>
        public string SubmissionTime;

        /// <summary>
        /// Timestamp when scan request completed.
        /// </summary>
        public string CompletionTime;
    }
}
