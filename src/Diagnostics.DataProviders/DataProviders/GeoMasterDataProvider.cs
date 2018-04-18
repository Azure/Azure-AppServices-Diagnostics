using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders
{

    public class GeoMasterDataProvider : DiagnosticDataProvider, IDiagnosticDataProvider
    {
        private readonly IGeoMasterClient _geoMasterClient;
        private GeoMasterDataProviderConfiguration _configuration;
        
        public GeoMasterDataProvider(OperationDataCache cache, GeoMasterDataProviderConfiguration configuration) : base(cache)
        {
            _configuration = configuration;
            _geoMasterClient = InitClient();
        }

        private IGeoMasterClient InitClient()
        {
            IGeoMasterClient geoMasterClient;
            bool onDiagRole = !string.IsNullOrWhiteSpace(_configuration.GeoCertThumbprint);
            if (onDiagRole)
            {
                geoMasterClient = new GeoMasterCertClient(_configuration);
            }
            else
            {
                geoMasterClient = new ArmClient(_configuration);
            }
            return geoMasterClient;
        }
        
        public async Task<IDictionary<string,string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name)
        {
            string path = SitePathUtility.GetSitePath(subscriptionId, resourceGroupName, name);
            path = path + "/config/appsettings/list";
            var geoMasterResponse = await HttpPost<GeoMasterResponse, string>(path);
            var properties = geoMasterResponse.Properties;
            Dictionary<string, string> appSettings = new Dictionary<string, string>();
            foreach (var item in properties)
            {
                if (item.Key.StartsWith("WEBSITE_"))
                {
                    appSettings.Add(item.Key, item.Value);
                }
            }
            return appSettings;
        }

        public async Task<IDictionary<string, string[]>> GetStickySlotSettingNames(string subscriptionId, string resourceGroupName, string name)
        {
            string path = SitePathUtility.GetSitePath(subscriptionId, resourceGroupName, name);
            path = path + "/config/slotConfigNames";
            GeoMasterResponse geoMasterResponse = null;
            geoMasterResponse = await HttpGet<GeoMasterResponse>(path);
            Dictionary<string, string[]> stickyToSlotSettings = new Dictionary<string, string[]>();
            foreach (var item in geoMasterResponse.Properties)
            {
                var val = new string[0];
                if (item.Value != null && item.Value is Newtonsoft.Json.Linq.JArray)
                {
                    var jArray = (Newtonsoft.Json.Linq.JArray)item.Value;
                    val = jArray.ToObject<string[]>();
                }
                stickyToSlotSettings.Add(item.Key, val);
            }
            return stickyToSlotSettings;
        }


        #region HttpMethods

        protected async Task<R> HttpGet<R>(string path, string queryString = "", string apiVersion = GeoMasterConstants.August2016Version, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = SitePathUtility.CsmAnnotateQueryString(queryString, apiVersion);
            var response = new HttpResponseMessage();

            try
            {
                var uri = path + query;
                response = await _geoMasterClient.Client.GetAsync(uri, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (TaskCanceledException)
            {
                if (cancellationToken != default(CancellationToken))
                {
                    throw new DataSourceCancelledException();
                }
                //if any task cancelled without provided cancellation token - we want capture exception in datasourcemanager
                throw;
            }

            if (typeof(R) == typeof(string))
            {
                return (await response.Content.ReadAsStringAsync()).CastTo<R>();
            }
            string responseContent = await response.Content.ReadAsStringAsync();
            R value = JsonConvert.DeserializeObject<R>(responseContent);
            return value;
        }
        
        protected async Task<R> HttpPost<R, T>(string path, T content = default(T) , string queryString = "", string apiVersion = GeoMasterConstants.August2016Version, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = SitePathUtility.CsmAnnotateQueryString(queryString, apiVersion);
            var response = new HttpResponseMessage();

            try
            {
                var uri = path + query;
                var body = JsonConvert.SerializeObject(content);
                response = await _geoMasterClient.Client.PostAsync(uri, new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (TaskCanceledException)
            {
                if (cancellationToken != default(CancellationToken))
                {
                    throw new DataSourceCancelledException();
                }
                //if any task cancelled without provided cancellation token - we want capture exception in datasourcemanager
                throw;
            }

            if (typeof(R) == typeof(string))
            {
                return (await response.Content.ReadAsStringAsync()).CastTo<R>();
            }
           
            string responseContent = await response.Content.ReadAsStringAsync();
            R value = JsonConvert.DeserializeObject<R>(responseContent);
            return value;
        }
        
        #endregion

    }

}
