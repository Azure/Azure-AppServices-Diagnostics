using Diagnostics.ModelsAndUtils.Models.ChangeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders.Interfaces
{
    public interface IChangeAnalysisClient
    {
        /// <summary>
        /// Gets the ARM Resource Id for given hostnames.
        /// </summary>
        /// <param name="hostnames">array of hostnames</param>
        /// <param name="subscription">subscription id</param>
        Task<ResourceIdResponseModel> GetResourceIdAsync(List<string> hostnames, string subscription);

        /// <summary>
        /// Get Change sets for a ResourceId.
        /// </summary>
        Task<List<ChangeSetResponseModel>> GetChangeSetsAsync(ChangeSetsRequest changeSetsRequest);

        /// <summary>
        /// Gets Changes for a ChangeSetId and ResourceId.
        /// </summary>
        Task<List<ResourceChangesResponseModel>> GetChangesAsync(ChangeRequest changeRequest);

        /// <summary>
        /// Gets the last scan time stamp for a resource.
        /// </summary>
        /// <param name="armResourceUri">Azure Resource Uri.</param>
        /// <returns>Last scan information.</returns>
        Task<LastScanResponseModel> GetLastScanInformation(string armResourceUri);

        /// <summary>
        /// Checks if a subscription has registered the ChangeAnalysis RP.
        /// </summary>
        /// <param name="subscriptionId">Subscription Id.</param>
        Task<SubscriptionOnboardingStatus> CheckSubscriptionOnboardingStatus(string subscriptionId);

        /// <summary>
        /// Submits scan request to Change Analysis RP or checks scan status.
        /// </summary>
        /// <param name="resourceId">Azure resource id</param>
        /// <param name="scanAction">Scan action: It is "submitscan" or "checkscan".</param>
        /// <returns>Contains info about the scan request with submissions state and time.</returns>
        Task<ChangeScanModel> ScanActionRequest(string resourceId, string scanAction);
    }
}
