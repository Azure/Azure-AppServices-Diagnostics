using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Diagnostics.DataProviders
{
    public class SupportObserverDataProvider : SupportObserverDataProviderBase
    {
        private object _lockObject = new object();

        public SupportObserverDataProvider(OperationDataCache cache, SupportObserverDataProviderConfiguration configuration, DataProviderContext dataProviderContext) : base(cache, configuration, dataProviderContext)
        {
        }

        public override Task<JObject> GetAdminSitesByHostNameAsync(string stampName, string[] hostNames)
        {
            throw new NotImplementedException();
        }

        public override Task<JObject> GetAdminSitesBySiteNameAsync(string stampName, string siteName)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<object>> GetAppServiceEnvironmentDeploymentsAsync(string hostingEnvironmentName)
        {
            throw new NotImplementedException();
        }

        public override Task<JObject> GetAppServiceEnvironmentDetailsAsync(string hostingEnvironmentName)
        {
            throw new NotImplementedException();
        }

        public override async Task<dynamic> GetCertificatesInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            var result = await Get($"subscriptions/{subscriptionName}/resourceGroups/{resourceGroupName}/certificates");
            return JsonConvert.DeserializeObject(result);
        }

        public override async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string siteName)
        {
            return await GetRuntimeSiteSlotMap(null, siteName);
        }

        public override async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName)
        {
            if (string.IsNullOrWhiteSpace(stampName))
            {
                throw new ArgumentNullException(nameof(stampName));
            }

            if (string.IsNullOrWhiteSpace(siteName))
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            return await GetRuntimeSiteSlotMapInternal(stampName, siteName, null);
        }

        public override async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName, string slotName)
        {
            if (string.IsNullOrWhiteSpace(stampName))
            {
                throw new ArgumentNullException(nameof(stampName));
            }

            if (string.IsNullOrWhiteSpace(siteName))
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            if (string.IsNullOrWhiteSpace(slotName))
            {
                throw new ArgumentNullException(nameof(slotName));
            }

            return await GetRuntimeSiteSlotMapInternal(stampName, siteName, slotName);
        }

        private async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMapInternal(string stampName, string siteName, string slotName)
        {
            var result = await GetObserverResource($"stamps/{stampName}/sites/{siteName}/runtimesiteslotmap");
            var slotTimeRangeCaseSensitiveDictionary = JsonConvert.DeserializeObject<Dictionary<string, List<RuntimeSitenameTimeRange>>>(result);
            var slotTimeRange = new Dictionary<string, List<RuntimeSitenameTimeRange>>(slotTimeRangeCaseSensitiveDictionary, StringComparer.CurrentCultureIgnoreCase);

            const string missingDataSignature = "SWAP HISTORY REMOVED";
            foreach (var slotMap in slotTimeRange)
            {
                var missingHistoricalSwapData = slotMap.Value.Where(swapInfo =>
                    swapInfo?.RuntimeSitename != null && swapInfo.RuntimeSitename.Equals(missingDataSignature, StringComparison.CurrentCultureIgnoreCase)
                );
                if (missingHistoricalSwapData.Any())
                {
                    var startTime = missingHistoricalSwapData.Min(x => x.StartTime);
                    var endTime = missingHistoricalSwapData.Max(x => x.EndTime);

                    if (!string.IsNullOrWhiteSpace(slotName) && slotMap.Key.Equals(slotName, StringComparison.CurrentCultureIgnoreCase) && missingHistoricalSwapData.Any(timeRange => timeRange.StartTime >= DataProviderContext.QueryStartTime) && missingHistoricalSwapData.Any(timeRange => timeRange.EndTime <= DataProviderContext.QueryEndTime))
                    {
                        Logger.LogDataProviderMessage(RequestId, "ObserverDataProvider", $"Warning. Swap historical data was purged for web app {siteName}({slotName})");
                    }

                    Logger.LogDataProviderMessage(RequestId, "ObserverDataProvider", $"Warning. No swap history data for web app {siteName}({slotMap.Key}) from {startTime} to {endTime}");
                }
            }

            return slotTimeRange;
        }

        public override async Task<dynamic> GetServerFarmsInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            var result = await Get($"subscriptions/{subscriptionName}/resourceGroups/{resourceGroupName}/serverfarms");
            return JsonConvert.DeserializeObject(result);
        }

        public override async Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm)
        {
            return await Get($"subscriptionid/{subscriptionId}/serverfarm/{serverFarm}/webspacename");
        }

        public override async Task<string> GetSiteResourceGroupNameAsync(string siteName)
        {
            return await Get($"sites/{siteName}/resourcegroupname");
        }

        public override async Task<dynamic> GetSitesInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            var sitesResult = await Get($"subscriptions/{subscriptionName}/resourceGroups/{resourceGroupName}/sites");
            return JsonConvert.DeserializeObject(sitesResult);
        }

        public override async Task<dynamic> GetSitesInServerFarmAsync(string subscriptionId, string serverFarmName)
        {
            var sitesResult = await Get($"subscriptionid/{subscriptionId}/serverfarm/{serverFarmName}/sites");
            return JsonConvert.DeserializeObject(sitesResult);
        }

        public override async Task<string> GetSiteWebSpaceNameAsync(string subscriptionId, string siteName)
        {
            return await Get($"subscriptionid/{subscriptionId}/sitename/{siteName}/webspacename");
        }

        public override Task<string> GetStorageVolumeForSiteAsync(string stampName, string siteName)
        {
            throw new NotImplementedException();
        }

        public override async Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName)
        {
            return await Get($"subscriptionid/{subscriptionId}/webspacename/{webSpaceName}/resourcegroupname");
        }

        public override async Task<dynamic> GetSite(string siteName)
        {
            return await GetSiteInternal(null, siteName, null);
        }

        public override async Task<dynamic> GetSite(string stampName, string siteName)
        {
            return await GetSiteInternal(stampName, siteName, null);
        }

        public override async Task<dynamic> GetSite(string stampName, string siteName, string slotName)
        {
            return await GetSiteInternal(stampName, siteName, slotName);
        }

        private async Task<dynamic> GetSiteInternal(string stampName, string siteName, string slotName)
        {
            string path = "";

            if (!string.IsNullOrWhiteSpace(stampName))
            {
                path = $"stamps/{stampName}/";
            }

            path = string.IsNullOrWhiteSpace(slotName) ? path + $"sites/{siteName}" : path + $"sites/{siteName}({slotName})";

            var response = await GetObserverResource(path);
            var siteObject = JsonConvert.DeserializeObject(response);
            return siteObject;
        }

        public override async Task<string> GetStampName(string subscriptionId, string resourceGroupName, string siteName)
        {
            dynamic siteObjects = await GetSite(siteName);
            JToken obj2 = ((JArray)siteObjects)
                    .Select(i => (JObject)i)
                    .FirstOrDefault(j =>
                        j.ContainsKey("subscription") &&
                        j["subscription"]["name"].ToString().Equals(
                            subscriptionId, StringComparison.InvariantCultureIgnoreCase
                        ) &&
                        j.ContainsKey("resource_group_name") && j["resource_group_name"].ToString().Equals(
                            resourceGroupName, StringComparison.InvariantCultureIgnoreCase
                        ) && j.ContainsKey("stamp")
                    );

            string stampName = obj2?["stamp"]?["name"]?.ToString();
            return stampName;
        }

        public override async Task<dynamic> GetHostNames(string stampName, string siteName)
        {
            var response = await Get($"stamps/{stampName}/sites/{siteName}/hostnames");
            var hostNames = JsonConvert.DeserializeObject(response);
            return hostNames;
        }

        public override async Task<dynamic> GetSitePostBody(string stampName, string siteName)
        {
            var response = await GetObserverResource($"stamps/{stampName}/sites/{siteName}/postbody");
            dynamic sitePostBody = JsonConvert.DeserializeObject(response);
            return sitePostBody;
        }

        public override async Task<dynamic> GetHostingEnvironmentPostBody(string hostingEnvironmentName)
        {
            var response = await GetObserverResource($"hostingEnvironments/{hostingEnvironmentName}/postbody");
            var hostingEnvironmentPostBody = JsonConvert.DeserializeObject(response);
            return hostingEnvironmentPostBody;
        }

        public override async Task<DataTable> ExecuteSqlQueryAsync(string cloudServiceName, string query)
        {
            if (!query.StartsWith("\""))
            {
                // BUG: Passing as JSON requires query to be wrapped in quotes, fix API expected content type
                query = $"\"{query}\"";
            }

            var loggingResponse = "Request succeeded";
            var route = $"/api/service/{cloudServiceName}/invokesql";

            var request = new HttpRequestMessage(HttpMethod.Post, route)
            {
                Content = new StringContent(query, Encoding.Default, "application/json")
            };

            var response = await SendObserverRequestAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException)
            {
                loggingResponse = result;
                throw;
            }
            finally
            {
                var logMessage = $"message:Observer SQL query sent, route:{route}, " +
                    $"query:{query}, statusCode:{(int)response.StatusCode}, response:{loggingResponse}";
                Logger.LogDataProviderMessage(RequestId, "ObserverDataProvider", logMessage);
            }

            return TryDeserializeDataTable(result);
        }

        private static DataTable TryDeserializeDataTable(string json)
        {
            DataTable datatable;
            try
            {
                datatable = (DataTable)JsonConvert.DeserializeObject(json, typeof(DataTable));
            }
            catch (JsonReaderException ex)
            {
                throw new Exception($"SQL Query did not return parsable JSON; response: \"{json}\"", ex);
            }

            return datatable;
        }

        public override HttpClient GetObserverClient()
        {
            // Instantiating an HttpClient class for every request will exhaust the number of sockets available under heavy loads
            // TODO: Use HttpClientFactory
            return new HttpClient();
        }

        /// <summary>
        /// Temporary solution.
        /// Remove when the following detectors don't use this codepath: Migration, ResourceGroupHealthCheck, SwapAnalysis.
        /// </summary>
        private async Task<string> Get(string path)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://support-bay-api.azurewebsites.net/observer/{path}?api-version=2.0");
            request.Headers.TryAddWithoutValidation("Authorization", await DataProviderContext.SupportBayApiObserverTokenService.GetAuthorizationTokenAsync());
            var response = await GetObserverClient().SendAsync(request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
    }
}
