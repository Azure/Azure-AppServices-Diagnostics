using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;

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

        public async Task<dynamic> GetResource(string observerUrl)
        {
            Uri uri;
            Dictionary<string, string> routeParametersAndValues = null;
            string routeTemplate = null;

            try
            {
                uri = new Uri(observerUrl);

                if (!uri.Host.Equals("wawsobserver.azurewebsites.windows.net", StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new FormatException($"{observerUrl} is not for an Observer call. Please use a URL that points to Observer. Eg., https://wawsobserver.azurewebsites.windows.net/Sites/mySite");
                }
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentNullException("observerUrl");
            }
            catch (UriFormatException ex)
            {
                string exceptionMessage = null;

                if (ex.Message.Contains("The URI is empty"))
                {
                    exceptionMessage = "ObserverUrl is empty. Please pass a non empty string for observerUrl";
                }

                //fix for travis ci
                if (!observerUrl.StartsWith("https://wawsobserver.azurewebsites.windows.net") || !observerUrl.StartsWith("http://wawsobserver.azurewebsites.windows.net"))
                {
                    throw new FormatException($"{observerUrl} is not for an Observer call. Please use a URL that points to Observer. Eg., https://wawsobserver.azurewebsites.windows.net/Sites/mySite");
                }

                exceptionMessage = "ObserverUrl is badly formatted. Please use correct format eg., https://wawsobserver.azurewebsites.windows.net/Sites/mySite";

                throw new FormatException(exceptionMessage);
            }

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
        public abstract Task<string> GetSiteResourceGroupName(string siteName);
        public abstract Task<dynamic> GetSitesInResourceGroup(string subscriptionName, string resourceGroupName);
        public abstract Task<dynamic> GetServerFarmsInResourceGroup(string subscriptionName, string resourceGroupName);
        public abstract Task<dynamic> GetCertificatesInResourceGroup(string subscriptionName, string resourceGroupName);
        public abstract Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName);
        public abstract Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm);
        public abstract Task<string> GetSiteWebSpaceName(string subscriptionId, string siteName);
        public abstract Task<dynamic> GetSitesInServerFarm(string subscriptionId, string serverFarmName);
        public abstract Task<JObject> GetAppServiceEnvironmentDetails(string hostingEnvironmentName);
        public abstract Task<IEnumerable<object>> GetAppServiceEnvironmentDeployments(string hostingEnvironmentName);
        public abstract Task<JObject> GetAdminSitesBySiteName(string stampName, string siteName);
        public abstract Task<JObject> GetAdminSitesByHostName(string stampName, string[] hostNames);
        public abstract Task<string> GetStorageVolumeForSite(string stampName, string siteName);
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
