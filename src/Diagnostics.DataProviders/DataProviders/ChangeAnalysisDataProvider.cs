using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.ModelsAndUtils.Models.ChangeAnalysis;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders
{
    public class ChangeAnalysisDataProvider : DiagnosticDataProvider, IChangeAnalysisDataProvider
    {
        private ChangeAnalysisDataProviderConfiguration dataProviderConfiguration;

        private ChangeAnalysisClient changeAnalysisClient;

        private string dataProviderRequestId;

        private KustoDataProvider kustoDataProvider;

        public ChangeAnalysisDataProvider(OperationDataCache cache, ChangeAnalysisDataProviderConfiguration configuration, KustoDataProviderConfiguration kustoConfig, string requestId, string clientObjectId, string principalName) : base(cache)
        {
            dataProviderConfiguration = configuration;
            dataProviderRequestId = requestId;
            changeAnalysisClient = new ChangeAnalysisClient(configuration, clientObjectId, principalName);
            kustoDataProvider = new KustoDataProvider(cache, kustoConfig, requestId);
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
                throw new Exception("Changes beyond last 14 days are not available. Please provide dates within last 14 days");
            }

            ChangeSetsRequest request = new ChangeSetsRequest
            {
                ResourceId = armResourceUri,
                StartTime = startTime,
                EndTime = endTime
            };

            // Get changeSet of the given arm resource uri
            return await changeAnalysisClient.GetChangeSetsAsync(request);
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
            DataTable kustoResultsTask = await kustoDataProvider.ExecuteQuery(query, stamp);
            List<string> hostnames = GetHostNamesFromTable(kustoResultsTask);
            ResourceIdResponseModel dependentResources = await changeAnalysisClient.GetResourceIdAsync(hostnames, subscriptionId);
            return dependentResources;
        }

        public DataProviderMetadata GetMetadata()
        {
            return new DataProviderMetadata
            {
                ProviderName = "ChangeAnalysis"
            };
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
    }
}
