namespace Diagnostics.ModelsAndUtils.Models.ChangeAnalysis
{
    /// <summary>
    /// This class captures information about when the last scan was run for a given Azure Resource.
    /// </summary>
    public class LastScanResponseModel
    {
        /// <summary>
        /// Azure Resource Id.
        /// </summary>
        public string ResourceId;

        /// <summary>
        /// Timestamp of last scan.
        /// </summary>
        public string TimeStamp;

        /// <summary>
        /// Source of last scan, it can be ARM, Site extension.
        /// </summary>
        public string Source;
    }
}
