using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Diagnostics.DataProviders
{
    public abstract class SupportObserverDataProviderBase : DiagnosticDataProvider, ISupportObserverDataProvider
    {
        protected readonly SupportObserverDataProviderConfiguration Configuration;
        protected readonly DataProviderContext DataProviderContext;
        protected readonly string RequestId;
        protected readonly DiagnosticsETWProvider Logger;

        protected static readonly Lazy<HttpClient> lazyClient = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        });

        public SupportObserverDataProviderBase(OperationDataCache cache, SupportObserverDataProviderConfiguration configuration, DataProviderContext dataProviderContext) : base(cache)
        {
            Configuration = configuration;
            RequestId = dataProviderContext.RequestId;
            DataProviderContext = dataProviderContext;
            Logger = DiagnosticsETWProvider.Instance;
        }

        public async Task<dynamic> GetResource(string resourceUrl)
        {
            if (string.IsNullOrWhiteSpace(resourceUrl))
                throw new ArgumentNullException(nameof(resourceUrl));

            Uri uri;

            var allowedHosts = new string[] { "wawsobserver.azurewebsites.windows.net", "wawsobserver-prod.azurewebsites.net", "wawsobserver-prod-staging.azurewebsites.net", "support-bay-api.azurewebsites.net", "support-bay-api-stage.azurewebsites.net", "localhost" };

            try
            {
                uri = new Uri(resourceUrl);

                if (!allowedHosts.Any(h => uri.Host.Equals(h, StringComparison.CurrentCultureIgnoreCase)))
                {
                    throw new FormatException($"Cannot make a call to {uri.Host}. Please use a URL that points to one of the hosts: {string.Join(',', allowedHosts)}");
                }
            }
            catch (UriFormatException ex)
            {
                // TODO: Fix for travis ci
                if (!allowedHosts.Any(h => resourceUrl.StartsWith($"https://{h}") || resourceUrl.StartsWith($"http://{h}")))
                {
                    throw new FormatException($"Please use a URL that points to one of the hosts: {string.Join(',', allowedHosts)}");
                }

                var exceptionMessage = "ResourceUrl is badly formatted. Please use correct format eg., https://wawsobserver.azurewebsites.windows.net/Sites/mySite";

                throw new FormatException(exceptionMessage, ex);
            }

            if (uri.Host.Contains(allowedHosts[0]) || uri.Host.Contains(allowedHosts[1]))
            {
                return await GetWawsObserverResourceAsync(uri);
            }
            else if (Configuration.ObserverLocalHostEnabled)
            {
                return await GetWawsObserverResourceAsync(ConvertToLocalObserverRoute(uri));
            }
            else
            {
                return await GetSupportObserverResourceAsync(uri);
            }
        }

        private static Uri ConvertToLocalObserverRoute(Uri uri)
        {
            if (!uri.AbsolutePath.StartsWith("/observer"))
            {
                return uri;
            }

            return new Uri(uri, uri.AbsolutePath.Remove(0, "/observer".Length));
        }

        private async Task<dynamic> GetWawsObserverResourceAsync(Uri uri)
        {
            var apiPath = uri.PathAndQuery.Substring(1);
            var response = await GetObserverResource(apiPath);
            var jObjectResponse = JsonConvert.DeserializeObject(response);
            return jObjectResponse;
        }

        private async Task<dynamic> GetSupportObserverResourceAsync(Uri uri)
        {
            var response = await GetObserverResource(uri.AbsoluteUri, Configuration.SupportBayApiObserverResourceId);
            var jObjectResponse = JsonConvert.DeserializeObject(response);
            return jObjectResponse;
        }

        protected async Task<string> GetObserverResource(string url, string resourceId = null)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            var cleanUrl = url.TrimStart('/');
            if (!cleanUrl.StartsWith("api"))
                url = $"api/{cleanUrl}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await SendObserverRequestAsync(request, resourceId);
            var result = await response.Content.ReadAsStringAsync();

            var loggingMessage = "Request succeeded";
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                ex.Data.Add("StatusCode", response.StatusCode);
                ex.Data.Add("ResponseContent", result);
                loggingMessage = result;
                throw;
            }
            finally
            {
                Logger.LogDataProviderMessage(RequestId, "ObserverDataProvider",
                    $"url:{new Uri(new Uri(Configuration.Endpoint), request.RequestUri)}, response:{loggingMessage}, statusCode:{(int)response.StatusCode}");

                request.Dispose();
                response.Dispose();
            }

            return result;
        }

        protected async Task<HttpResponseMessage> SendObserverRequestAsync(HttpRequestMessage request, string resourceId = null, HttpClient httpClient = null)
        {
            if (httpClient == null)
            {
                httpClient = lazyClient.Value;
            }

            request.Headers.TryAddWithoutValidation(HeaderConstants.RequestIdHeaderName, RequestId);
            if (!Configuration.ObserverLocalHostEnabled)
            {
                request.Headers.TryAddWithoutValidation("Authorization", await GetToken(resourceId));
            }

            request.RequestUri = new Uri(new Uri(Configuration.Endpoint), request.RequestUri);
            var response = await httpClient.SendAsync(request);

            return response;
        }

        private async Task<string> GetToken(string resourceId)
        {
            if (!string.IsNullOrWhiteSpace(resourceId) && resourceId.Equals(Configuration.SupportBayApiObserverResourceId))
            {
                return await DataProviderContext.SupportBayApiObserverTokenService.GetAuthorizationTokenAsync();
            }
            else
            {
                return await DataProviderContext.WawsObserverTokenService.GetAuthorizationTokenAsync();
            }
        }

        public abstract Task<dynamic> GetSite(string siteName);

        public abstract Task<dynamic> GetSite(string stampName, string siteName);

        public abstract Task<dynamic> GetSite(string stampName, string siteName, string slotName);

        public abstract Task<string> GetStampName(string subscriptionId, string resourceGroupName, string siteName);

        public abstract Task<dynamic> GetHostNames(string stampName, string siteName);

        public abstract Task<dynamic> GetSitePostBody(string stampName, string siteName);

        public abstract Task<dynamic> GetHostingEnvironmentPostBody(string hostingEnvironmentName);

        public abstract Task<string> GetSiteResourceGroupNameAsync(string siteName);

        public abstract Task<dynamic> GetSitesInResourceGroupAsync(string subscriptionName, string resourceGroupName);

        public abstract Task<dynamic> GetServerFarmsInResourceGroupAsync(string subscriptionName, string resourceGroupName);

        public abstract Task<dynamic> GetCertificatesInResourceGroupAsync(string subscriptionName, string resourceGroupName);

        public abstract Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName);

        public abstract Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm);

        public abstract Task<string> GetSiteWebSpaceNameAsync(string subscriptionId, string siteName);

        public abstract Task<dynamic> GetSitesInServerFarmAsync(string subscriptionId, string serverFarmName);

        public abstract Task<JArray> GetAdminSitesAsync(string siteName);

        public abstract Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName);

        public abstract Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName, string slotName);

        public abstract Task<DataTable> ExecuteSqlQueryAsync(string cloudServiceName, string query);

        public abstract HttpClient GetObserverClient();

        public DataProviderMetadata GetMetadata()
        {
            return null;
        }
    }
}
