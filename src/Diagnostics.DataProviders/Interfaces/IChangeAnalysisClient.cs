namespace Diagnostics.DataProviders.Interfaces
{
    public interface IChangeAnalysisClient
    {
        /// <summary>
        /// Gets the ARM Resource Id for given hostnames
        /// </summary>
        /// <param name="hostnames"></param>
        /// <param name="subscription"></param>
        void GetResoureceIdAsync(string[] hostnames, string subscription);

        /// <summary>
        /// Get Change sets for a ResoureceId
        /// </summary>
        void GetChangeSetsAsync();

        /// <summary>
        /// Gets Changes for a ResoureceId
        /// </summary>
        void GetChangesAsync();
    }
}
