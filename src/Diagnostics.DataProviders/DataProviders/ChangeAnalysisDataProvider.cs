using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.ChangeAnalysis;
using Diagnostics.Logger;
using Microsoft.AspNetCore.Http;

namespace Diagnostics.DataProviders
{
    public class ChangeAnalysisDataProvider : DiagnosticDataProvider, IChangeAnalysisDataProvider
    {
        private ChangeAnalysisDataProviderConfiguration dataProviderConfiguration;

        private ChangeAnalysisClient changeAnalysisClient;

        private string dataProviderRequestId;

        private IKustoDataProvider kustoDataProvider;
        
        public ChangeAnalysisDataProvider(OperationDataCache cache, ChangeAnalysisDataProviderConfiguration configuration, string requestId, string clientObjectId, string principalName, IKustoDataProvider kustoDataProvider, IHeaderDictionary incomingRequestHeaders) : base(cache, configuration)
        {
            dataProviderConfiguration = configuration;
            dataProviderRequestId = requestId;
            changeAnalysisClient = new ChangeAnalysisClient(configuration, requestId, clientObjectId, incomingRequestHeaders, principalName);
            this.kustoDataProvider = kustoDataProvider;
        }

        /// <summary>
        /// Get all change sets for the given arm resource uri.
        /// </summary>
        /// <param name="armResourceUri">ARM Resource URI.</param>
        /// <param name="startTime">Start time of time range.</param>
        /// <param name="endTime">End time of the time range.</param>
        /// <returns>List of changesets.</returns>
        public async Task<List<ChangeSetResponseModel>> GetChangeSetsForResource(string armResourceUri, DateTime startTime, DateTime endTime)
        {
            if (string.IsNullOrWhiteSpace(armResourceUri))
            {
                throw new ArgumentNullException(nameof(armResourceUri));
            }

            // Validation for date range as the data retention policy is 14 days.
            DateTime maxDataRetentionDate = DateTime.Now.AddDays(-14);
            if (startTime < maxDataRetentionDate)
            {
                throw new ArgumentException("Changes beyond last 14 days are not available. Please provide dates within last 14 days");
            }

            ChangeSetsRequest request = new ChangeSetsRequest
            {
                ResourceId = armResourceUri,
                StartTime = startTime,
                EndTime = endTime
            };

            // Get changeSet of the given arm resource uri
            List<ChangeSetResponseModel> changesets = await changeAnalysisClient.GetChangeSetsAsync(request);
            changesets = changesets.OrderByDescending(change => change.ChangeSetTime).ToList();
            if (changesets.Count > 0)
            {
                var latestChange = changesets[0];
                latestChange.LastScanInformation = await changeAnalysisClient.GetLastScanInformation(armResourceUri);
                latestChange.ResourceChanges = await changeAnalysisClient.GetChangesAsync(new ChangeRequest
                {
                    ChangeSetId = latestChange.ChangeSetId,
                    ResourceId = latestChange.ResourceId
                });
            }
            return changesets;
        }

        /// <summary>
        /// Get all Changes for a given changesetId.
        /// </summary>
        /// <param name="changeSetId">ChangeSetId retrieved from <see cref="GetChangeSetsForResource(string, DateTime, DateTime)"/>.</param>
        /// <param name="resourceUri">ARM Resource Uri.</param>
        /// <returns>List of changes.</returns>
        public async Task<List<ResourceChangesResponseModel>> GetChangesByChangeSetId(string changeSetId, string resourceUri)
        {
            if (string.IsNullOrWhiteSpace(changeSetId))
            {
                throw new ArgumentNullException(nameof(changeSetId));
            }

            if (string.IsNullOrWhiteSpace(resourceUri))
            {
                throw new ArgumentNullException(nameof(resourceUri));
            }

            DiagnosticsETWProvider.Instance.LogDataProviderMessage(dataProviderRequestId, "ChangeAnalysisDataProvider", $"Changeset id before calling api : {changeSetId}");
            ChangeRequest request = new ChangeRequest
            {
                ResourceId = resourceUri,
                ChangeSetId = changeSetId
            };

            return await changeAnalysisClient.GetChangesAsync(request);
        }

        /// <summary>
        /// Gets ARM Resource Uri and Hostname for all dependent resources for a given site name.
        /// </summary>
        /// <param name="sitename">Name of the site.</param>
        /// <param name="subscriptionId">Azure Subscription Id.</param>
        /// <param name="stamp">Stamp where the site is hosted.</param>
        /// <param name="startTime">End time of the time range.</param>
        /// <param name="endTime">Start time of the time range.</param>
        /// <returns>List of ARM resource uri for dependent resources of the site.</returns>
        public async Task<ResourceIdResponseModel> GetDependentResourcesAsync(string sitename, string subscriptionId, string stamp, string startTime, string endTime)
        {
            // Query Kusto to get Dependencies
            string query =
                $@"
                    let startTime = datetime({startTime});
                    let endTime = datetime({endTime});
                    let period = 30m;
                    let sitename = ""{sitename}"";
                    AntaresRuntimeWorkerSandboxEvents
                    | where TIMESTAMP  < endTime and TIMESTAMP >= ago(20d) and Sandbox =~ sitename and EventId == 60011
                    | extend CustomProcessId = iff(ImagePath == 'w3wp.exe' , Pid , NewProcessId )
                    | summarize by Role, RoleInstance, CustomProcessId , EventStampName, Tenant
                    | join (DNSQueryThirtyMinuteTable | extend CustomProcessId = Pid)
                     on Role, RoleInstance, EventStampName, CustomProcessId, Tenant
                    | where TIMESTAMP > (startTime - period) and TIMESTAMP < endTime
                    | distinct QueryName
                  ";
            DataTable kustoResults = await kustoDataProvider.ExecuteQuery(query, stamp);
            List<string> hostnames = GetHostNamesFromTable(kustoResults);
            ResourceIdResponseModel dependentResources = await changeAnalysisClient.GetResourceIdAsync(hostnames, subscriptionId);
            return dependentResources;
        }

        /// <summary>
        /// Gets the last scan time stamp for a resource.
        /// </summary>
        /// <param name="armResourceUri">Azure Resource Uri.</param>
        /// <returns>Last scan information.</returns>
        public async Task<LastScanResponseModel> GetLastScanInformation(string armResourceUri)
        {
            if (string.IsNullOrWhiteSpace(armResourceUri))
            {
                throw new ArgumentNullException(nameof(armResourceUri));
            }

            return await changeAnalysisClient.GetLastScanInformation(armResourceUri);
        }

        /// <summary>
        /// Checks if a subscription has registered the ChangeAnalysis RP.
        /// </summary>
        /// <param name="subscriptionId">Subscription Id.</param>
        public async Task<SubscriptionOnboardingStatus> GetSubscriptionOnboardingStatus(string subscriptionId)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            return await changeAnalysisClient.CheckSubscriptionOnboardingStatus(subscriptionId);
        }

        public DataProviderMetadata GetMetadata()
        {
            return null;
        }

        /// <summary>
        /// Submits a scan request to Change Analysis RP.
        /// </summary>
        /// <param name="resourceId">Azure resource id</param>
        /// <returns>Contains info about the scan request with submissions state and time.</returns>
        public async Task<ChangeScanModel> ScanActionRequest(string resourceId, string scanAction)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            if (string.IsNullOrWhiteSpace(scanAction))
            {
                throw new ArgumentNullException(nameof(scanAction));
            }

            return await changeAnalysisClient.ScanActionRequest(resourceId, scanAction);
        }

        private List<string> GetHostNamesFromTable(DataTable kustoTable)
        {
            List<string> hostnames = new List<string>();
            if (kustoTable != null && kustoTable.Rows.Count > 0)
            {
                foreach (DataRow row in kustoTable.Rows)
                {
                    hostnames.Add(row["QueryName"].ToString());
                }
            }

            return hostnames;
        }

        /// <summary>
        /// Forwards the request to Change Analysis Client
        /// </summary>
        /// <param name="requestUri">Request URI</param>
        /// <param name="postBody">Post body</param>
        /// <param name="method">HTTP Method.</param>
        /// <returns>JSON string</returns>
        public async Task<string> InvokeChangeAnalysisRequest(string requestUri, object postBody = null, HttpMethod method = null)
        {
            return await changeAnalysisClient.PrepareAndSendRequest(requestUri, postBody, method);
        }
    }
}
