using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;

namespace Diagnostics.DataProviders
{

    public class GeoMasterDataProvider : DiagnosticDataProvider, IDiagnosticDataProvider, IGeoMasterDataProvider
    {
        private readonly IGeoMasterClient _geoMasterClient;
        private GeoMasterDataProviderConfiguration _configuration;
        
        private string[] SensitiveAppSettingsEndingWith = new string[] { "CONNECTIONSTRING", "_SECRET", "_KEY", "_ID", "_CONTENTSHARE", "TOKEN_STORE", "TOKEN" };

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

        /// <summary>
        /// Gets all the APP SETTINGS for the Web App that start with WEBSITE_ filtering out
        /// the sensitive settings like connectionstrings, tokens, secrets, keys, content shares etc.
        /// </summary>
        /// <param name="subscriptionId">Subscription Id for the resource</param>
        /// <param name="resourceGroupName">The resource group that the resource is part of </param>
        /// <param name="name">Name of the resource</param>
        /// <returns>A dictionary of AppSetting Keys and values</returns>
        /// <example>
        /// <code>
        /// This sample shows how to call the <see cref="GetAppSettings"/> method in a detector
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     var subId = cxt.Resource.SubscriptionId;
        ///     var rg = cxt.Resource.ResourceGroup;
        ///     var name = cxt.Resource.Name;
        ///     
        ///     var appSettings = await dp.GeoMaster.GetAppSettings(subId, rg, name);
        ///     foreach(var key in appSettings.Keys)
        ///     {
        ///         // do something with the appSettingValue
        ///         string appSettingValue = appSettings[key];
        ///     }
        /// }
        /// </code>
        /// </example>
        public async Task<IDictionary<string,string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name)
        {
            string path = $"{SitePathUtility.GetSitePath(subscriptionId, resourceGroupName, name)}/config/appsettings/list";
            var geoMasterResponse = await HttpPost<GeoMasterResponse, string>(path);
            var properties = geoMasterResponse.Properties;
            Dictionary<string, string> appSettings = new Dictionary<string, string>();
            foreach (var item in properties)
            {
                if (item.Key.StartsWith("WEBSITE_"))
                {
                    if (!SensitiveAppSettingsEndingWith.Any(x => item.Key.EndsWith(x)))
                    {
                        appSettings.Add(item.Key, item.Value);
                    }
                }
            }
            return appSettings;
        }

        /// <summary>
        /// Gets all App Settings that are marked sticky to the slot for this site\slot
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="name"></param>
        /// <returns>A dictionary of Settings that are marked sticky to the slot</returns>
        /// <example>
        /// This sample shows how to call the <see cref="GetStickySlotSettingNames"/> method in a detector
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     var subId = cxt.Resource.SubscriptionId;
        ///     var rg = cxt.Resource.ResourceGroup;
        ///     var name = cxt.Resource.Name;
        ///     
        ///     var stickySettings = await dp.GeoMaster.GetStickySlotSettingNames(subId, rg, name);
        ///     foreach(var key in stickySettings.Keys)
        ///     {
        ///         // do something with the stickyslot value
        ///         string[] settings = stickySettings[key];
        ///     }
        /// }
        /// </code>
        /// </example>
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

        /// <summary>
        /// Gets the results of running the VnetVerifier tool for the hosting environment
        /// </summary>
        /// <param name="subscriptionId">Subscription Id for the resource</param>
        /// <param name="vnetResourceGroup">The resource group in which the VNet is a part of</param>
        /// <param name="vnetName">Name of the VNET</param>
        /// <param name="vnetSubnetName">Subnet name of the VNET</param>
        /// <param name="cancellationToken">(Optional)</param>
        /// <returns></returns>
        /// <example>
        /// This sample shows how you can call this function to get the NSG rules that failed or succeeded
        /// for this App Service environment
        /// <code>
        ///  public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     
        ///     //Get VNET information from observer
        ///     var name = cxt.Resource.InternalName;
        ///     var url = $"https://wawsobserver.azurewebsites.windows.net/minienvironments/{name}"
        ///     
        ///     var hostingEnvironmentData = await dp.Observer.GetResource(url);
        ///     var vnetName = (string)hostingEnvironmentData.vnet_name;
        ///     var subnet = (string)hostingEnvironmentData.vnet_subnet_name;
        ///     var vnetRg = (string)hostingEnvironmentData.vnet_resource_group;        
        ///     var subId = cxt.Resource.SubscriptionId;
        ///     
        ///     var results = await dp.GeoMaster.VerifyHostingEnvironmentVnet(subId, 
        ///                   vnetRg, 
        ///                   vnetName, 
        ///                   subnet);
        ///                   
        ///     foreach(var failedTest in results.FailedTests)
        ///     {
        ///         var testName = failedTest.TestName
        ///     }
        /// }
        /// </code>
        /// </example>
        public async Task<VnetValidationRespone> VerifyHostingEnvironmentVnet(string subscriptionId, string vnetResourceGroup, string vnetName, string vnetSubnetName, CancellationToken cancellationToken = default(CancellationToken))
        {            
            var path = string.Format(@"subscriptions/{0}/providers/Microsoft.Web/verifyHostingEnvironmentVnet", subscriptionId);
            var vnetParameters = new VnetParameters { VnetResourceGroup = vnetResourceGroup, VnetName = vnetName, VnetSubnetName = vnetSubnetName };
            var result = await HttpPost<VnetValidationRespone, VnetParameters>(path, vnetParameters, "", GeoMasterConstants.March2016Version, cancellationToken);
            return result;
        }

        /// <summary>
        /// Gets a dictionary of all the deployments that were triggered for this Web App. The key of this dictionary 
        /// is the DeploymentId
        /// </summary>
        /// <param name="subscriptionId">Subscription Id for the resource</param>
        /// <param name="resourceGroupName">The resource group that the resource is part of </param>
        /// <param name="name">Name of the resource</param>
        /// <returns></returns>
        /// <example>
        /// The below example shows how you call <see cref="GetAppDeployments"/> to find out details about the deployments that were triggered for this App.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     var subId = cxt.Resource.SubscriptionId;
        ///     var rg = cxt.Resource.ResourceGroup;
        ///     var name = cxt.Resource.Name;
        ///     
        ///     var deployments = await dp.GeoMaster.GetAppDeployments(subId, rg, name);
        ///     foreach(var deployment in deployments)
        ///     {
        ///         string deploymentId = deployment["id"].ToString();
        ///         var message = "DeploymentId = " +  deploymentId;
        ///         
        ///         // get a specific property like this (Hint - View all properties 
        ///         // at https://resources.azure.com)
        ///         string deployer = deployment["deployer"].ToString();
        ///         
        ///         // or just loop through all the keys
        ///         foreach(string key in deployment.Keys)
        ///         {
        ///             var deploymentinfo = $" {key} = {deployment[key]}";
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public async Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name)
        {
            string path = $"{SitePathUtility.GetSitePath(subscriptionId, resourceGroupName, name)}/deployments";
            GeoMasterResponseArray geoMasterResponse = null;
            geoMasterResponse = await HttpGet<GeoMasterResponseArray>(path);
            var deployments = new List<IDictionary<string, dynamic>> ();
            foreach (var deployment in geoMasterResponse.Value)
            {
                deployments.Add(deployment.Properties);
            }
            return deployments;
        }

        /// <summary>
        /// All the ARM or GeoMaster operations that are allowed over HTTP GET can be called via this method by passing the path. 
        /// To get a list of all the HTTP GET based ARM operations, check out a WebApp on https://resources.azure.com
        /// It should be noted that the response of the ARM operation is of 3 types 
        /// 1) Response contains a Properties{} object.
        /// 2) Reponse contains a Value[] array which has a properties object.
        /// 3) Response contains a Value[] array that has no properties object. 
        /// 
        /// To invoke the right route, pass the right class to the method call i.e. GeoMasterResponse or GeoMasterResponseArray or GeoMasterResponseDynamicArray
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subscriptionId">Subscription Id for the resource</param>
        /// <param name="resourceGroupName">The resource group that the resource is part of </param>
        /// <param name="name">Name of the resource</param>
        /// <param name="path">The path to the API route (for e.g. usages, recommendations)</param>
        /// <example>
        /// The below shows how to make this method call to invoke the different types of operations
        /// <code>
        ///  public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        /// 
        ///     var subId = cxt.Resource.SubscriptionId;
        ///     var rg = cxt.Resource.ResourceGroup;
        ///     var name = cxt.Resource.Name;
        ///     
        ///     var resp = await dp.GeoMaster.MakeHttpGetRequest<![CDATA[<GeoMasterResponse>]]>(subId, 
        ///                rg, 
        ///                name, 
        ///                "sourcecontrols/web");
        ///     
        ///     var repoUrl =  resp.Properties["repoUrl"];
        ///     var branch = resp.Properties["branch"];
        ///     var provisioningState = resp.Properties["provisioningState"];
        ///             
        ///     var respVal = await dp.GeoMaster.MakeHttpGetRequest<![CDATA[<GeoMasterResponseDynamicArray>]]>(subId, 
        ///                   rg, 
        ///                   name, 
        ///                   "usages");
        ///     
        ///     foreach(var val in respVal.Value)
        ///     {
        ///         var unit = val.unit;
        ///         var name = val.name.value;
        ///         var localizedValue = val.name.localizedValue;
        ///         var currentValue = val.currentValue;
        ///     }
        ///     
        ///     // To get properties on the site root path, just call the method like this
        ///     var siteProperties = await dp.GeoMaster.MakeHttpGetRequest<![CDATA[<GeoMasterResponse>]]>(subId, 
        ///                rg, 
        ///                name);
        ///     foreach(var item in siteProperties.Properties)
        ///     {
        ///         string str = item.Key + " " + item.Value;
        ///     }
        /// }
        /// </code>
        /// </example>
        public async Task<T> MakeHttpGetRequest<T>(string subscriptionId, string resourceGroupName, string name, string path = "")
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                path = path.StartsWith("/") ? path.Substring(1) : path;
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                path = $"{SitePathUtility.GetSitePath(subscriptionId, resourceGroupName, name)}";
            }
            else
            {
                path = $"{SitePathUtility.GetSitePath(subscriptionId, resourceGroupName, name)}/{path}";
            }

            var geoMasterResponse = await HttpGet<T>(path);
            return geoMasterResponse;
        }

        /// <summary>
        /// All the ARM or GeoMaster operations that are allowed over HTTP GET can be called via this method by passing the full path e.g. subscriptions/{subscriptionId}/providers/Microsoft.Web/certificates
        /// To get a list of all the HTTP GET based ARM operations, check out a WebApp on https://resources.azure.com
        /// It should be noted that the response of the ARM operation is of 3 types 
        /// 1) Response contains a Properties{} object.
        /// 2) Reponse contains a Value[] array which has a properties object.
        /// 3) Response contains a Value[] array that has no properties object. 
        /// 
        /// To invoke the right route, pass the right class to the method call i.e. GeoMasterResponse or GeoMasterResponseArray or GeoMasterResponseDynamicArray
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fullPath">Full path to the ARM operation</param>
        /// <example>
        /// <code>
        ///  public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        /// 
        ///     // HTTP GET operations supported by Antares Resource Provider only are supported
        ///     // For e.g. Microsoft.CertificateRegistration endpoints are available on our 
        ///     // GeoMaster like this
        ///     
        ///     var fullPath = "/sharedResourceProviderBase/certificateRegistration/subscriptions/{subscriptionId}/providers/Microsoft.CertificateRegistration/certificateOrders";        
        ///     var resp = await dp.GeoMaster.MakeHttpGetRequest<![CDATA[<GeoMasterResponseArray>]]>(fullPath, "", "2015-08-01");
        ///     
        ///     foreach (var cert in resp.Value)
        ///     {
        ///         string distinguisedName = cert.Properties["distinguishedName"];
        ///     }
        ///        
        /// }
        /// </code>
        /// </example>
        /// <returns></returns>
        public async Task<T> MakeHttpGetRequestWithFullPath<T>(string fullPath, string queryString ="", string apiVersion = GeoMasterConstants.August2016Version)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                throw new ArgumentNullException(fullPath);
            }

            var geoMasterResponse = await HttpGet<T>(fullPath, queryString, apiVersion);
            return geoMasterResponse;
        }

        #region HttpMethods

        private async Task<R> HttpGet<R>(string path, string queryString = "", string apiVersion = GeoMasterConstants.August2016Version, CancellationToken cancellationToken = default(CancellationToken))
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
        
        private async Task<R> HttpPost<R, T>(string path, T content = default(T) , string queryString = "", string apiVersion = GeoMasterConstants.August2016Version, CancellationToken cancellationToken = default(CancellationToken))
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

        public DataProviderMetadata GetMetadata()
        {
            return null;
        }

        #endregion

    }

}
