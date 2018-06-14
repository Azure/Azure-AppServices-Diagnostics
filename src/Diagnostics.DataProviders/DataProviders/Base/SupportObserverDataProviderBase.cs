using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Diagnostics.DataProviders
{
    public abstract class SupportObserverDataProviderBase : DiagnosticDataProvider, ISupportObserverDataProvider
    {
        protected readonly SupportObserverDataProviderConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private List<string> _routeTemplates;

        public SupportObserverDataProviderBase(OperationDataCache cache, SupportObserverDataProviderConfiguration configuration) : base(cache)
        {
            _configuration = configuration;
            _httpClient = GetObserverClient();
            _httpClient.BaseAddress = new Uri("https://wawsobserver-prod.azurewebsites.net/api/");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            FillObserverRouteTemplates();
        }

        public Task<dynamic> GetResource(string resourceUrl)
        {
            Uri uri;

            var allowedHosts = new string[] { "wawsobserver.azurewebsites.windows.net", "support-bay-api.azurewebsites.net", "support-bay-api-stage.azurewebsites.net" };

            try
            {
                uri = new Uri(resourceUrl);

                if (!allowedHosts.Any(h => uri.Host.Equals(h, StringComparison.CurrentCultureIgnoreCase)))
                {
                    throw new FormatException($"Cannot make a call to {uri.Host}. Please use a URL that points to one of the hosts: {string.Join(',', allowedHosts)}");
                }
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentNullException("resourceUrl");
            }
            catch (UriFormatException ex)
            {
                string exceptionMessage = null;

                if (ex.Message.Contains("The URI is empty"))
                {
                    exceptionMessage = "ResourceUrl is empty. Please pass a non empty string for resourceUrl";
                }

                //fix for travis ci
                if (!resourceUrl.StartsWith("https://wawsobserver.azurewebsites.windows.net") || !resourceUrl.StartsWith("http://wawsobserver.azurewebsites.windows.net"))
                if (!allowedHosts.Any(h => resourceUrl.StartsWith($"https://{h}") || resourceUrl.StartsWith($"http://{h}")))
                {
                    throw new FormatException($"Please use a URL that points to one of the hosts: {string.Join(',', allowedHosts)}");
                }

                exceptionMessage = "ResourceUrl is badly formatted. Please use correct format eg., https://wawsobserver.azurewebsites.windows.net/Sites/mySite";

                throw new FormatException(exceptionMessage);
            }

            if (uri.Host.Contains(allowedHosts[0]))
            {
                return GetWawsObserverResourceAsync(uri);
            }
            else
            {
                return GetSupportObserverResourceAsync(uri);
            }
        }

        private async Task<dynamic> GetWawsObserverResourceAsync(Uri uri)
        {
            Dictionary<string, string> routeParametersAndValues = null;
            string routeTemplate = null;

            //take a substring to remove forward-slash from start of PathAndQuery
            var pathAndQuery = uri.PathAndQuery.Substring(1);
            foreach (var template in _routeTemplates)
            {
                if (TryMatchRoute(pathAndQuery, template, out routeParametersAndValues))
                {
                    routeTemplate = template;
                    break;
                }
            }

            if (routeParametersAndValues == null)
            {
                throw new ArgumentException("Obsever may not have an API for your observerUrl");
            }

            var apiPath = CreateObserverQueryAndPath(routeParametersAndValues, routeTemplate);
            var response = await GetObserverResource(apiPath);

            var jObjectResponse = JsonConvert.DeserializeObject(response);
            return jObjectResponse;
        }

        private async Task<dynamic> GetSupportObserverResourceAsync(Uri uri)
        {
            var response = await GetObserverResource(uri.AbsoluteUri, _configuration.RuntimeSiteSlotMapResourceUri);
            var jObjectResponse = JsonConvert.DeserializeObject(response);
            return jObjectResponse;
        }

        protected async Task<string> GetObserverResource(string url, string resourceId = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _configuration.GetAccessToken(resourceId));
            var cancelToken = new CancellationToken();
            var response = await _httpClient.SendAsync(request, cancelToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        public abstract Task<dynamic> GetSite(string siteName);
        public abstract Task<dynamic> GetSite(string stampName, string siteName);
        public abstract Task<string> GetSiteResourceGroupNameAsync(string siteName);
        public abstract Task<dynamic> GetSitesInResourceGroupAsync(string subscriptionName, string resourceGroupName);
        public abstract Task<dynamic> GetServerFarmsInResourceGroupAsync(string subscriptionName, string resourceGroupName);
        public abstract Task<dynamic> GetCertificatesInResourceGroupAsync(string subscriptionName, string resourceGroupName);
        public abstract Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName);
        public abstract Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm);
        public abstract Task<string> GetSiteWebSpaceNameAsync(string subscriptionId, string siteName);
        public abstract Task<dynamic> GetSitesInServerFarmAsync(string subscriptionId, string serverFarmName);
        public abstract Task<JObject> GetAppServiceEnvironmentDetailsAsync(string hostingEnvironmentName);
        public abstract Task<IEnumerable<object>> GetAppServiceEnvironmentDeploymentsAsync(string hostingEnvironmentName);
        public abstract Task<JObject> GetAdminSitesBySiteNameAsync(string stampName, string siteName);
        public abstract Task<JObject> GetAdminSitesByHostNameAsync(string stampName, string[] hostNames);
        public abstract Task<string> GetStorageVolumeForSiteAsync(string stampName, string siteName);
        public abstract Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string siteName);
        public abstract Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName);
        public abstract HttpClient GetObserverClient();

        private void FillObserverRouteTemplates()
        {
            if (_routeTemplates == null)
            {

                _routeTemplates = new List<string>
                {
                    "sites/{siteName}",
                    "stamps/{stampName}/sites/{siteName}",
                    "deletedsites/{siteName}",
                    "stamps/{stampName}/sites/deleted/{siteName}",
                    "webspaces/{webspaceGeoId}",
                    "subscriptions/{subscriptionName}/resourceGroups/{resourceGroupName}",
                    "subscriptions/{subscriptionName}",
                    "subscriptions/id/{subscriptionId}",
                    "stamps/{stampName}/subscriptions/{subscriptionName}",
                    "stamps/{stampName}",
                    "certificates/{thumbprint}",
                    "subscriptions/{subscriptionName}/certificates",
                    "domains/{domainName}",
                    "subscriptions/{subscriptionName}/domains",
                    "certificateorders/{certificateOrderName}",
                    "subscriptions/{subscriptionName}/certificateorders",
                    "minienvironments/{environmentName}",
                    "stamps/{stampName}/storagevolumes/{volumeName}",
                    "stamps/{stampName}/serverfarms/id/{serverFarmId}",
                };
            }
        }

        private bool TryMatchRoute(string inputUrl, string routeTemplate, out Dictionary<string, string> routeParametersAndValues)
        {
            var inputUrlArr = inputUrl.Split(new char[] { '/' });
            var routeTemplateArr = routeTemplate.Split(new char[] { '/' });

            Dictionary<string, string> _tmpRouteParametersAndValues = new Dictionary<string, string>();
            routeParametersAndValues = null;

            if (inputUrlArr.Length == routeTemplateArr.Length)
            {
                for (int i = 0; i < inputUrlArr.Length; i++)
                {
                    if (routeTemplateArr[i].Contains("{") && routeTemplateArr[i].Contains("}"))
                    {
                        var key = routeTemplateArr[i].Replace("{", string.Empty).Replace("}", string.Empty).ToLower();
                        _tmpRouteParametersAndValues.Add(key, inputUrlArr[i]);
                        continue;
                    }
                    else if (!routeTemplateArr[i].Equals(inputUrlArr[i], StringComparison.CurrentCultureIgnoreCase))
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }

            routeParametersAndValues = _tmpRouteParametersAndValues;

            return true;
        }

        private string CreateObserverQueryAndPath(Dictionary<string, string> routeParametersAndValues, string routeTemplate)
        {
            routeTemplate = routeTemplate.ToLower();
            foreach (var item in routeParametersAndValues)
            {
                routeTemplate = routeTemplate.Replace("{" + item.Key + "}", item.Value);
            }

            return routeTemplate;
        }
    }
}
