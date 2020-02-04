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
        public SupportObserverDataProvider(OperationDataCache cache, SupportObserverDataProviderConfiguration configuration, DataProviderContext dataProviderContext) : base(cache, configuration, dataProviderContext)
        {
        }

        private async Task<TObject> DeserializeResponseAsync<TObject>(string path) where TObject : new()
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            var response = await GetObserverResource(path);

            return response == null ?
                new TObject() :
                JsonConvert.DeserializeObject<TObject>(response);
        }

        public override async Task<JArray> GetAdminSitesAsync(string siteName)
        {
            if (string.IsNullOrWhiteSpace(siteName))
                throw new ArgumentNullException(nameof(siteName));

            var path = $"sites/{siteName}/adminsites";

            return await DeserializeResponseAsync<JArray>(path);
        }

        public override async Task<JArray> GetAdminSitesAsync(string siteName, string stampName)
        {
            if (string.IsNullOrWhiteSpace(stampName))
                throw new ArgumentNullException(nameof(stampName));
            if (string.IsNullOrWhiteSpace(siteName))
                throw new ArgumentNullException(nameof(siteName));

            var path = $"stamps/{stampName}/sites/{siteName}/adminsites";

            return await DeserializeResponseAsync<JArray>(path);
        }

        public override async Task<dynamic> GetCertificatesInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            if (string.IsNullOrWhiteSpace(subscriptionName))
                throw new ArgumentNullException(nameof(subscriptionName));
            if (string.IsNullOrWhiteSpace(resourceGroupName))
                throw new ArgumentNullException(nameof(resourceGroupName));

            var result = await Get($"subscriptions/{subscriptionName}/resourceGroups/{resourceGroupName}/certificates");
            return JsonConvert.DeserializeObject(result);
        }

        public override Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName, DateTime? endTime = null)
        {
            return GetRuntimeSiteSlotMap(stampName, siteName, null, endTime);
        }

        public override async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName, string slotName = null, DateTime? endTime = null)
        {
            return await GetRuntimeSiteSlotMapInternal(stampName, siteName, slotName, endTime);
        }

        public override async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(OperationContext<App> cxt, string stampName = "", string siteName = "", string slotName = "", DateTime? endTime = null)
        {
            if (cxt?.Resource?.Stamp == null)
                throw new ArgumentNullException(nameof(cxt));

            stampName = string.IsNullOrWhiteSpace(stampName) ? cxt.Resource.Stamp.InternalName : stampName;
            siteName = string.IsNullOrWhiteSpace(siteName) ? cxt.Resource.Name : siteName;
            endTime = endTime == null ? DateTime.SpecifyKind(DateTime.Parse(cxt.EndTime), DateTimeKind.Utc) : endTime;

            return await GetRuntimeSiteSlotMapInternal(stampName, siteName, slotName, endTime);
        }

        private async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMapInternal(string stampName, string siteName, string slotName, DateTime? endTime = null)
        {
            if (string.IsNullOrWhiteSpace(stampName))
                throw new ArgumentNullException(nameof(stampName));
            if (string.IsNullOrWhiteSpace(siteName))
                throw new ArgumentNullException(nameof(siteName));

            var route = $"stamps/{stampName}/sites/{siteName}/runtimesiteslotmap";
            if (endTime != null)
                route = $"{route}?endTime={endTime}";

            var result = await GetObserverResource(route);
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
                    var slotStartTime = missingHistoricalSwapData.Min(x => x.StartTime);
                    var slotEndTime = missingHistoricalSwapData.Max(x => x.EndTime);

                    if (!string.IsNullOrWhiteSpace(slotName) && slotMap.Key.Equals(slotName, StringComparison.CurrentCultureIgnoreCase) && missingHistoricalSwapData.Any(timeRange => timeRange.StartTime >= DataProviderContext.QueryStartTime) && missingHistoricalSwapData.Any(timeRange => timeRange.EndTime <= DataProviderContext.QueryEndTime))
                    {
                        Logger.LogDataProviderMessage(RequestId, "ObserverDataProvider", $"Warning. Swap historical data was purged for web app {siteName}({slotName})");
                    }

                    Logger.LogDataProviderMessage(RequestId, "ObserverDataProvider", $"Warning. No swap history data for web app {siteName}({slotMap.Key}) from {slotStartTime} to {slotEndTime}");
                }
            }

            return slotTimeRange;
        }

        public override async Task<dynamic> GetServerFarmsInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            if (string.IsNullOrWhiteSpace(subscriptionName))
                throw new ArgumentNullException(nameof(subscriptionName));
            if (string.IsNullOrWhiteSpace(resourceGroupName))
                throw new ArgumentNullException(nameof(resourceGroupName));

            var result = await Get($"subscriptions/{subscriptionName}/resourceGroups/{resourceGroupName}/serverfarms");
            return JsonConvert.DeserializeObject(result);
        }

        public override async Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
                throw new ArgumentNullException(nameof(subscriptionId));
            if (string.IsNullOrWhiteSpace(serverFarm))
                throw new ArgumentNullException(nameof(serverFarm));

            return await Get($"subscriptionid/{subscriptionId}/serverfarm/{serverFarm}/webspacename");
        }

        public override async Task<string> GetSiteResourceGroupNameAsync(string siteName)
        {
            if (string.IsNullOrWhiteSpace(siteName))
                throw new ArgumentNullException(nameof(siteName));

            return await Get($"sites/{siteName}/resourcegroupname");
        }

        public override async Task<dynamic> GetSitesInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            if (string.IsNullOrWhiteSpace(subscriptionName))
                throw new ArgumentNullException(nameof(subscriptionName));
            if (string.IsNullOrWhiteSpace(resourceGroupName))
                throw new ArgumentNullException(nameof(resourceGroupName));

            var sitesResult = await Get($"subscriptions/{subscriptionName}/resourceGroups/{resourceGroupName}/sites");
            return JsonConvert.DeserializeObject(sitesResult);
        }

        public override async Task<dynamic> GetSitesInServerFarmAsync(string subscriptionId, string serverFarmName)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
                throw new ArgumentNullException(nameof(subscriptionId));
            if (string.IsNullOrWhiteSpace(serverFarmName))
                throw new ArgumentNullException(nameof(serverFarmName));

            var sitesResult = await Get($"subscriptionid/{subscriptionId}/serverfarm/{serverFarmName}/sites");
            return JsonConvert.DeserializeObject(sitesResult);
        }

        public override async Task<string> GetSiteWebSpaceNameAsync(string subscriptionId, string siteName)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
                throw new ArgumentNullException(nameof(subscriptionId));
            if (string.IsNullOrWhiteSpace(siteName))
                throw new ArgumentNullException(nameof(siteName));

            return await Get($"subscriptionid/{subscriptionId}/sitename/{siteName}/webspacename");
        }

        public override async Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
                throw new ArgumentNullException(nameof(subscriptionId));
            if (string.IsNullOrWhiteSpace(webSpaceName))
                throw new ArgumentNullException(nameof(webSpaceName));

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
            if (string.IsNullOrWhiteSpace(siteName))
                throw new ArgumentNullException(nameof(siteName));

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
            if (string.IsNullOrWhiteSpace(subscriptionId))
                throw new ArgumentNullException(nameof(subscriptionId));
            if (string.IsNullOrWhiteSpace(resourceGroupName))
                throw new ArgumentNullException(nameof(resourceGroupName));

            var siteObjects = await GetAdminSitesAsync(siteName);
            if (siteObjects == null || !siteObjects.Any())
                throw new Exception($"Could not get admin sites for site {siteName}");

            var icic = StringComparison.InvariantCultureIgnoreCase;
            var objects = siteObjects.Select(x => (JObject)x)
                .Where(x => x.ContainsKey("Subscription") && x["Subscription"].ToString().Equals(subscriptionId, icic))
                .Where(x => x.ContainsKey("ResourceGroupName") && x["ResourceGroupName"].ToString().Equals(resourceGroupName, icic));

            var internalStampObjects = objects.Where(x => x.ContainsKey("InternalStampName") && !string.IsNullOrWhiteSpace(x["InternalStampName"].ToString()));
            if (internalStampObjects.Any())
                return internalStampObjects.First()["InternalStampName"].ToString();

            var stampNameObjects = objects.Where(x => x.ContainsKey("StampName") && !string.IsNullOrWhiteSpace(x["StampName"].ToString()));
            if (stampNameObjects.Any())
                return stampNameObjects.First()["StampName"].ToString();

            throw new Exception($"Admin Sites response did not contain stamp name for site {siteName}. Admin Sites response: {siteObjects}");
        }

        public override async Task<dynamic> GetHostNames(string stampName, string siteName)
        {
            if (string.IsNullOrWhiteSpace(stampName))
                throw new ArgumentNullException(nameof(stampName));
            if (string.IsNullOrWhiteSpace(siteName))
                throw new ArgumentNullException(nameof(siteName));

            var response = await Get($"stamps/{stampName}/sites/{siteName}/hostnames");
            var hostNames = JsonConvert.DeserializeObject(response);
            return hostNames;
        }

        public override async Task<dynamic> GetSitePostBody(string stampName, string siteName)
        {
            if (string.IsNullOrWhiteSpace(stampName))
                throw new ArgumentNullException(nameof(stampName));
            if (string.IsNullOrWhiteSpace(siteName))
                throw new ArgumentNullException(nameof(siteName));

            var response = await GetObserverResource($"stamps/{stampName}/sites/{siteName}/postbody");
            dynamic sitePostBody = JsonConvert.DeserializeObject(response);
            return sitePostBody;
        }

        public override async Task<dynamic> GetHostingEnvironmentPostBody(string hostingEnvironmentName)
        {
            if (string.IsNullOrWhiteSpace(hostingEnvironmentName))
                throw new ArgumentNullException(nameof(hostingEnvironmentName));

            var response = await GetObserverResource($"hostingEnvironments/{hostingEnvironmentName}/postbody");
            var hostingEnvironmentPostBody = JsonConvert.DeserializeObject(response);
            return hostingEnvironmentPostBody;
        }

        public override async Task<DataTable> ExecuteSqlQueryAsync(string cloudServiceName, string query)
        {
            if (string.IsNullOrWhiteSpace(cloudServiceName))
                throw new ArgumentNullException(nameof(cloudServiceName));
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

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
                    $"query:{query}, statusCode:{response.StatusCode}, response:{loggingResponse}";
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
            return lazyClient.Value;
        }

        /// <summary>
        /// Temporary solution.
        /// Remove when the following detectors don't use this codepath: Migration, ResourceGroupHealthCheck, SwapAnalysis.
        /// </summary>
        private async Task<string> Get(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://support-bay-api.azurewebsites.net/observer/{path}?api-version=2.0");
            request.Headers.TryAddWithoutValidation("Authorization", await DataProviderContext.SupportBayApiObserverTokenService.GetAuthorizationTokenAsync());
            var response = await GetObserverClient().SendAsync(request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
    }
}
