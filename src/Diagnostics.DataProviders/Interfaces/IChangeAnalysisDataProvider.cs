using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.ChangeAnalysis;

namespace Diagnostics.DataProviders.Interfaces
{
    public interface IChangeAnalysisDataProvider
    {
        /// <summary>
        /// Gets ARM Resource Uri and Hostname for all dependent resources for a given site name.
        /// </summary>
        /// <param name="sitename">Name of the site.</param>
        /// <param name="subscriptionId">Azure Subscription Id.</param>
        /// <param name="stamp">Stamp where the site is hosted.</param>
        /// <param name="startTime">End time of the time range.</param>
        /// <param name="endTime">Start time of the time range.</param>
        /// <returns>List of ARM resource uri for dependent resources of the site.</returns>
        Task<ResourceIdResponseModel> GetDependentResourcesAsync(string sitename, string subscriptionId, string stamp, string startTime, string endTime);

        /// <summary>
        /// Get all change sets for the given arm resource uri.
        /// </summary>
        /// <param name="armResourceUri">ARM Resource URI.</param>
        /// <param name="startTime">Start time of time range.</param>
        /// <param name="endTime">End time of the time range.</param>
        /// <returns>List of changesets.</returns>
        Task<List<ChangeSetResponseModel>> GetChangeSetsForResource(string armResourceUri, DateTime startTime, DateTime endTime);

        /// <summary>
        /// Get all Changes for a given changesetId.
        /// </summary>
        /// <param name="changeSetId">ChangeSetId retrieved from <see cref="GetChangeSetsForResource(string, DateTime, DateTime)"/>.</param>
        /// <param name="resourceUri">ARM Resource Uri.</param>
        /// <returns>List of changes.</returns>
        Task<List<ResourceChangesResponseModel>> GetChangesByChangeSetId(string changeSetId, string resourceUri);

        /// <summary>
        /// Gets the last scan time stamp for a resource.
        /// </summary>
        /// <param name="armResourceUri">Azure Resource Uri.</param>
        /// <returns>Last scan information.</returns>
        Task<LastScanResponseModel> GetLastScanInformation(string armResourceUri);

        DataProviderMetadata GetMetadata();

        /// <summary>
        /// Checks if a subscription has registered the ChangeAnalysis RP.
        /// </summary>
        /// <param name="subscriptionId">Subscription Id.</param>
        Task<SubscriptionOnboardingStatus> GetSubscriptionOnboardingStatus(string subscriptionId);

        /// <summary>
        /// Submits scan request to Change Analysis RP or checks scan status.
        /// </summary>
        /// <param name="resourceId">Azure resource id</param>
        /// <param name="scanAction">Scan action: It is "submitscan" or "checkscan".</param>
        /// <returns>Contains info about the scan request with submissions state and time.</returns>
        Task<ChangeScanModel> ScanActionRequest(string resourceId, string scanAction);

        /// <summary>
        /// Forwards the request to Change Analysis Client
        /// </summary>
        /// <param name="requestUri">Request URI</param>
        /// <param name="postBody">Post body</param>
        /// <param name="method">HTTP Method.</param>
        /// <returns>JSON string</returns>
        Task<string> InvokeChangeAnalysisRequest(string requestUri, object postBody = null, HttpMethod method = null);
    }
}
