using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json.Linq;

namespace Diagnostics.DataProviders
{
    internal class DataProviderLogDecorator : IKustoDataProvider, IGeoMasterDataProvider, ISupportObserverDataProvider, IAppInsightsDataProvider
    {
        IKustoDataProvider _kustoDataProvider;
        IGeoMasterDataProvider _geomasterDataProvider;
        ISupportObserverDataProvider _observerDataProvider;
        IAppInsightsDataProvider _appInsightsDataProvider;

        DataProviderMetadata _currentMetadataProvider;

        public DataProviderLogDecorator(IKustoDataProvider dataProvider)
        {
            _kustoDataProvider = dataProvider;
            _currentMetadataProvider = dataProvider.GetMetadata();
        }

        public DataProviderLogDecorator(IGeoMasterDataProvider dataProvider)
        {
            _geomasterDataProvider = dataProvider;
            _currentMetadataProvider = dataProvider.GetMetadata();
        }

        public DataProviderLogDecorator(ISupportObserverDataProvider dataProvider)
        {
            _observerDataProvider = dataProvider;
            _currentMetadataProvider = dataProvider.GetMetadata();
        }

        public DataProviderLogDecorator(IAppInsightsDataProvider dataProvider)
        {
            _appInsightsDataProvider = dataProvider;
            _currentMetadataProvider = dataProvider.GetMetadata();
        }

        public Task<DataTable> ExecuteAppInsightsQuery(string query)
        {
            return MakeDependencyCall(null, _appInsightsDataProvider.ExecuteAppInsightsQuery(query));
        }

        public Task<bool> SetAppInsightsKey(string appId, string apiKey)
        {
            return MakeDependencyCall(null, _appInsightsDataProvider.SetAppInsightsKey(appId, apiKey));
        }

        public Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, string operationName = null)
        {
            return MakeDependencyCall(requestId, _kustoDataProvider.ExecuteQuery(query, stampName, requestId, operationName));
        }

        public Task<DataTable> ExecuteClusterQuery(string query, string requestId = null, string operationName = null)
        {
            return ExecuteQuery(query, DataProviderConstants.FakeStampForAnalyticsCluster, requestId, operationName);
        }

        public Task<JObject> GetAdminSitesByHostNameAsync(string stampName, string[] hostNames)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetAdminSitesByHostNameAsync(stampName, hostNames));
        }

        public Task<JObject> GetAdminSitesBySiteNameAsync(string stampName, string siteName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetAdminSitesBySiteNameAsync(stampName, siteName));
        }

        public Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name)
        {
            return GetAppDeployments(subscriptionId, resourceGroupName, name, GeoMasterConstants.ProductionSlot);
        }
        public Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name, string slotName)
        {
            return MakeDependencyCall(null, _geomasterDataProvider.GetAppDeployments(subscriptionId, resourceGroupName, name, slotName));
        }

        public Task<IEnumerable<object>> GetAppServiceEnvironmentDeploymentsAsync(string hostingEnvironmentName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetAppServiceEnvironmentDeploymentsAsync(hostingEnvironmentName));
        }

        public Task<JObject> GetAppServiceEnvironmentDetailsAsync(string hostingEnvironmentName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetAppServiceEnvironmentDetailsAsync(hostingEnvironmentName));
        }

        public Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name)
        {
            return GetAppSettings(subscriptionId, resourceGroupName, name, GeoMasterConstants.ProductionSlot);
        }
        public Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name, string slotName = GeoMasterConstants.ProductionSlot)
        {
            return MakeDependencyCall(null, _geomasterDataProvider.GetAppSettings(subscriptionId, resourceGroupName, name, slotName));
        }

        public Task<dynamic> GetCertificatesInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetCertificatesInResourceGroupAsync(subscriptionName, resourceGroupName));
        }

        public DataProviderMetadata GetMetadata()
        {
            return _currentMetadataProvider;
        }

        public Task<dynamic> GetResource(string wawsObserverUrl)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetResource(wawsObserverUrl));
        }

        public Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string siteName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetRuntimeSiteSlotMap(siteName));
        }

        public Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetRuntimeSiteSlotMap(stampName, siteName));
        }

        public Task<dynamic> GetServerFarmsInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetServerFarmsInResourceGroupAsync(subscriptionName, resourceGroupName));
        }

        public Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetServerFarmWebspaceName(subscriptionId, serverFarm));
        }

        public Task<dynamic> GetSite(string siteName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetSite(siteName));
        }

        public Task<dynamic> GetSite(string stampName, string siteName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetSite(stampName, siteName));
        }

        public Task<string> GetStampName(string subscriptionId, string resourceGroupName, string siteName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetStampName(subscriptionId, resourceGroupName, siteName));
        }

        public Task<dynamic> GetHostNames(string stampName, string siteName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetHostNames(stampName, siteName));
        }

        public Task<dynamic> GetSitePostBody(string stampName, string siteName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetSitePostBody(stampName, siteName));
        }

        public Task<dynamic> GetHostingEnvironmentPostBody(string hostingEnvironmentName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetHostingEnvironmentPostBody(hostingEnvironmentName));
        }

        public Task<string> GetSiteResourceGroupNameAsync(string siteName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetSiteResourceGroupNameAsync(siteName));
        }

        public Task<dynamic> GetSitesInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetSitesInResourceGroupAsync(subscriptionName, resourceGroupName));
        }

        public Task<dynamic> GetSitesInServerFarmAsync(string subscriptionId, string serverFarmName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetSitesInServerFarmAsync(subscriptionId, serverFarmName));
        }

        public Task<string> GetSiteWebSpaceNameAsync(string subscriptionId, string siteName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetSiteWebSpaceNameAsync(subscriptionId, siteName));
        }

        public Task<IDictionary<string, string[]>> GetStickySlotSettingNames(string subscriptionId, string resourceGroupName, string name)
        {
            return MakeDependencyCall(null, _geomasterDataProvider.GetStickySlotSettingNames(subscriptionId, resourceGroupName, name));
        }

        public Task<string> GetStorageVolumeForSiteAsync(string stampName, string siteName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetStorageVolumeForSiteAsync(stampName, siteName));
        }

        public Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName)
        {
            return MakeDependencyCall(null, _observerDataProvider.GetWebspaceResourceGroupName(subscriptionId, webSpaceName));
        }

        public Task<T> MakeHttpGetRequest<T>(string subscriptionId, string resourceGroupName, string name, string slotName, string path = "")
        {
            return MakeDependencyCall(null, _geomasterDataProvider.MakeHttpGetRequest<T>(subscriptionId, resourceGroupName, name, slotName, path));
        }
        public Task<T> MakeHttpGetRequest<T>(string subscriptionId, string resourceGroupName, string name, string path = "")
        {
            return MakeHttpGetRequest<T>(subscriptionId, resourceGroupName, name, GeoMasterConstants.ProductionSlot, path);
        }

        public Task<T> MakeHttpGetRequestWithFullPath<T>(string fullPath, string queryString = "", string apiVersion = GeoMasterConstants.August2016Version)
        {
            return MakeDependencyCall(null, _geomasterDataProvider.MakeHttpGetRequestWithFullPath<T>(fullPath, queryString, apiVersion));
        }

        public Task<string> GetLinuxContainerLogs(string subscriptionId, string resourceGroupName, string name, string slotName)
        {
            return MakeDependencyCall(null, _geomasterDataProvider.GetLinuxContainerLogs(subscriptionId, resourceGroupName, name, slotName));
        }

        public Task<VnetValidationRespone> VerifyHostingEnvironmentVnet(string subscriptionId, string vnetResourceGroup, string vnetName, string vnetSubnetName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return MakeDependencyCall(null, _geomasterDataProvider.VerifyHostingEnvironmentVnet(subscriptionId, vnetResourceGroup, vnetName, vnetSubnetName, cancellationToken));
        }

        private async Task<T> MakeDependencyCall<T>(string requestId, Task<T> dataProviderTask, [CallerMemberName]string dataProviderOperation = "")
        {
            Exception dataProviderException = null;
            DateTime startTime = DateTime.UtcNow, endTime;
            try
            {
                return await dataProviderTask;
            }
            catch (Exception ex)
            {
                dataProviderException = ex;
                throw;
            }
            finally
            {
                endTime = DateTime.UtcNow;
                var latencyMilliseconds = Convert.ToInt64((endTime - startTime).TotalMilliseconds);

                if (dataProviderException != null)
                {
                    DiagnosticsETWProvider.Instance.LogDataProviderException(requestId ?? "", dataProviderOperation, 
                        startTime.ToString("HH:mm:ss.fff"), endTime.ToString("HH:mm:ss.fff"), 
                        latencyMilliseconds, dataProviderException.GetType().ToString(), dataProviderException.ToString());
                }
                else
                {
                    DiagnosticsETWProvider.Instance.LogDataProviderOperationSummary(requestId ?? "", dataProviderOperation, startTime.ToString("HH:mm:ss.fff"), 
                        endTime.ToString("HH:mm:ss.fff"), latencyMilliseconds);
                }
            }
        }
    }
}
