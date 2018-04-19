using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Diagnostics.DataProviders
{
    public class SupportObserverDataProvider : DiagnosticDataProvider, ISupportObserverDataProvider
    {
        private readonly SupportObserverDataProviderConfiguration _configuration;
        private static AuthenticationContext _authContext;
        private static ClientCredential _aadCredentials;
        private readonly HttpClient _httpClient;
        private object _lockObject = new object();
        private List<string> _routeTemplates;

        public SupportObserverDataProvider(OperationDataCache cache, SupportObserverDataProviderConfiguration configuration) : base(cache)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://wawsobserver-prod.azurewebsites.net/api/");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _routeTemplates = new List<string>();
        }

        public Task<JObject> GetAdminSitesByHostName(string stampName, string[] hostNames)
        {
            throw new NotImplementedException();
        }

        public Task<JObject> GetAdminSitesBySiteName(string stampName, string siteName)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<object>> GetAppServiceEnvironmentDeployments(string hostingEnvironmentName)
        {
            throw new NotImplementedException();
        }

        public Task<JObject> GetAppServiceEnvironmentDetails(string hostingEnvironmentName)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Dictionary<string, string>>> GetCertificatesInResourceGroup(string subscriptionName, string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string siteName)
        {
            return await GetRuntimeSiteSlotMap(null, siteName);
        }

        public async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName)
        {
            return await GetRuntimeSiteSlotMapInternal(stampName, siteName);
        }

        private async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMapInternal(string stampName, string siteName)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, string.IsNullOrWhiteSpace(stampName) ? $"/sites/{siteName}/runtimesiteslotmap" : $"stamp/{stampName}/sites/{siteName}/runtimesiteslotmap");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessToken(_configuration.RuntimeSiteSlotMapResourceUri));
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var slotTimeRangeCaseSensitiveDictionary = JsonConvert.DeserializeObject<Dictionary<string, List<RuntimeSitenameTimeRange>>>(result);
            var slotTimeRange = new Dictionary<string, List<RuntimeSitenameTimeRange>>(slotTimeRangeCaseSensitiveDictionary, StringComparer.CurrentCultureIgnoreCase);
            return slotTimeRange;
        }

        public Task<IEnumerable<Dictionary<string, string>>> GetServerFarmsInResourceGroup(string subscriptionName, string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetSiteResourceGroupName(string siteName)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Dictionary<string, string>>> GetSitesInResourceGroup(string subscriptionName, string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Dictionary<string, string>>> GetSitesInServerFarm(string subscriptionId, string serverFarmName)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetSiteWebSpaceName(string subscriptionId, string siteName)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetStorageVolumeForSite(string stampName, string siteName)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName)
        {
            throw new NotImplementedException();
        }

        public async Task<dynamic> GetSite(string siteName)
        {
            var response = await GetObserverResource($"sites/{siteName}");
            var siteObject = JsonConvert.DeserializeObject(response);
            return siteObject;
        }

        public async Task<dynamic> GetSite(string stampName, string siteName)
        {
            var response = await GetObserverResource($"stamps/{stampName}/sites/{siteName}");
            var siteObject = JsonConvert.DeserializeObject(response);
            return siteObject;
        }

        public async Task<dynamic> GetResource(string observerUrl)
        {
            Uri uri;

            try
            {
                if (string.IsNullOrWhiteSpace(observerUrl))
                {
                    throw new ArgumentNullException("observerUrl");
                }



                uri = new Uri(observerUrl);

                if (!uri.Host.Equals("wawsobserver.azurewebsites.windows.net", StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new FormatException($"{observerUrl} is not for an Observer call. Please use a URL that points to Observer. Eg., https://wawsobserver.azurewebsites.windows.net/Sites/mySite");
                }

            }catch (UriFormatException ex)
            {
                string exceptionMessage = null;

                if (ex.Message.Contains("The URI is empty"))
                {
                    exceptionMessage = "ObserverUrl is empty. Please pass a non empty string for observerUrl";
                }

                exceptionMessage = "ObserverUrl is badly formatted. Please use correct format eg., https://wawsobserver.azurewebsites.windows.net/Sites/mySite";

                throw new FormatException(exceptionMessage);
            }

            //take a substring to remove forward-slash from start of PathAndQuery
            var response = await GetObserverResource(uri.PathAndQuery.Substring(1));

            var jObjectResponse = JsonConvert.DeserializeObject(response);
            return jObjectResponse;
        }

        private async Task<string> GetObserverResource(string url, string resourceId = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessToken(resourceId));
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        private async Task<string> GetAccessToken(string resourceId = null)
        {
            if (_authContext == null)
            {
                lock (_lockObject)
                {
                    if (_authContext == null)
                    {
                        _aadCredentials = new ClientCredential(_configuration.ClientId, _configuration.AppKey);
                        _authContext = new AuthenticationContext("https://login.microsoftonline.com/microsoft.onmicrosoft.com", TokenCache.DefaultShared);
                    }
                }
            }

            var authResult = await _authContext.AcquireTokenAsync(resourceId ?? _configuration.ResourceId, _aadCredentials);
            return authResult.AccessToken;
        }

        private void FillObserverRouteTemplates()
        {
            if (_routeTemplates == null)
            {
                _routeTemplates = new List<string>
                {
                    "api/sites/{siteName}",
                    "api/stamps/{stampName}/sites/{siteName}",
                    "api/deletedsites/{siteName}",
                    "api/stamps/{stampName}/sites/deleted/{siteName}",
                    "api/webspaces/{webspaceGeoId}",
                    "api/subscriptions/{subscriptionName}/resourceGroups/{resourceGroupName}",
                    "api/subscriptions/{subscriptionName}",
                    "api/subscriptions/id/{subscriptionId}",
                    "api/stamps/{stampName}/subscriptions/{subscriptionName}",
                    "api/stamps/{stampName}",
                    "api/certificates/{thumbprint}",
                    "api/subscriptions/{subscriptionName}/certificates",
                    "api/domains/{domainName}",
                    "api/subscriptions/{subscriptionName}/domains",
                    "api/certificateorders/{certificateOrderName}",
                    "api/subscriptions/{subscriptionName}/certificateorders",
                    "api/minienvironments/{environmentName}",
                    "api/stamps/{stampName}/storagevolumes/{volumeName}",
                    "api/stamps/{stampName}/serverfarms/id/{serverFarmId}",
                };
            }
        }
    }
}
