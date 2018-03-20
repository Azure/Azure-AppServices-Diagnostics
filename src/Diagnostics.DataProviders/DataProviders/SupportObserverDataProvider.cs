using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;

namespace Diagnostics.DataProviders
{
    public class SupportObserverDataProvider : DiagnosticDataProvider, ISupportObserverDataProvider
    {
        private readonly SupportObserverDataProviderConfiguration _configuration;
        private static AuthenticationContext _authContext;
        private static ClientCredential _aadCredentials;
        private readonly HttpClient _httpClient;

        public SupportObserverDataProvider(OperationDataCache cache, SupportObserverDataProviderConfiguration configuration) : base(cache)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://support-bay-api.azurewebsites.net/observer");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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

        public Task<JObject> GetRuntimeSiteSlotMap(string siteName)
        {
            throw new NotImplementedException();
        }

        public Task<JObject> GetRuntimeSiteSlotMap(string stampName, string siteName)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Dictionary<string, string>>> GetServerFarmsInResourceGroup(string subscriptionName, string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<string>> GetSiteHostNames(string siteName)
        {
            var response = await GetObserverResource($"/sites/{siteName}/hostnames?api-version=2.0");
            var hostnames = response.Split(new char[] { ',' });
            return hostnames;
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

        private async Task<string> GetObserverResource(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessToken());
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        private async Task<string> GetAccessToken()
        {
            if (_authContext == null)
            {
                _aadCredentials = new ClientCredential(_configuration.ClientId, _configuration.AppKey);
                _authContext = new AuthenticationContext("https://login.microsoftonline.com/microsoft.onmicrosoft.com", TokenCache.DefaultShared);
            }

            var authResult = await _authContext.AcquireTokenAsync(_configuration.ResourceUri, _aadCredentials);
            return authResult.AccessToken;
        }
    }
}
