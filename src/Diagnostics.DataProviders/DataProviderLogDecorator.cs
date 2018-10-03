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
        private IKustoDataProvider _kustoDataProvider;
        private IGeoMasterDataProvider _geomasterDataProvider;
        private ISupportObserverDataProvider _observerDataProvider;
        private IAppInsightsDataProvider _appInsightsDataProvider;
        private DataProviderMetadata _currentMetadataProvider;
        private string _requestId;
        private CancellationToken _dataSoureCancellationToken;

        public DataProviderLogDecorator(DataProviderContext context, IKustoDataProvider dataProvider) : this(context, dataProvider.GetMetadata())
        {
            _kustoDataProvider = dataProvider;
        }

        public DataProviderLogDecorator(DataProviderContext context, IGeoMasterDataProvider dataProvider) : this(context, dataProvider.GetMetadata())
        {
            _geomasterDataProvider = dataProvider;
        }

        public DataProviderLogDecorator(DataProviderContext context, ISupportObserverDataProvider dataProvider) : this(context, dataProvider.GetMetadata())
        {
            _observerDataProvider = dataProvider;
        }

        public DataProviderLogDecorator(DataProviderContext context, IAppInsightsDataProvider dataProvider) : this(context, dataProvider.GetMetadata())
        {
            _appInsightsDataProvider = dataProvider;
        }

        private DataProviderLogDecorator(DataProviderContext context, DataProviderMetadata metaData)
        {
            _currentMetadataProvider = metaData;
            _requestId = context.RequestId;
            _dataSoureCancellationToken = context.DataSourcesCancellationToken;
        }

        public Task<DataTable> ExecuteAppInsightsQuery(string query)
        {
            return MakeDependencyCall(_appInsightsDataProvider.ExecuteAppInsightsQuery(query));
        }

        public Task<bool> SetAppInsightsKey(string appId, string apiKey)
        {
            return MakeDependencyCall(_appInsightsDataProvider.SetAppInsightsKey(appId, apiKey));
        }

        public Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, string operationName = null)
        {
            return MakeDependencyCall(_kustoDataProvider.ExecuteQuery(query, stampName, requestId, operationName), requestId);
        }

        public Task<DataTable> ExecuteClusterQuery(string query, string requestId = null, string operationName = null)
        {
            return ExecuteQuery(query, DataProviderConstants.FakeStampForAnalyticsCluster, requestId, operationName);
        }

        public Task<JObject> GetAdminSitesByHostNameAsync(string stampName, string[] hostNames)
        {
            return MakeDependencyCall(_observerDataProvider.GetAdminSitesByHostNameAsync(stampName, hostNames));
        }

        public Task<JObject> GetAdminSitesBySiteNameAsync(string stampName, string siteName)
        {
            return MakeDependencyCall(_observerDataProvider.GetAdminSitesBySiteNameAsync(stampName, siteName));
        }

        public Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name)
        {
            return GetAppDeployments(subscriptionId, resourceGroupName, name, GeoMasterConstants.ProductionSlot);
        }
        public Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name, string slotName)
        {
            return MakeDependencyCall(_geomasterDataProvider.GetAppDeployments(subscriptionId, resourceGroupName, name, slotName));
        }

        public Task<IEnumerable<object>> GetAppServiceEnvironmentDeploymentsAsync(string hostingEnvironmentName)
        {
            return MakeDependencyCall(_observerDataProvider.GetAppServiceEnvironmentDeploymentsAsync(hostingEnvironmentName));
        }

        public Task<JObject> GetAppServiceEnvironmentDetailsAsync(string hostingEnvironmentName)
        {
            return MakeDependencyCall(_observerDataProvider.GetAppServiceEnvironmentDetailsAsync(hostingEnvironmentName));
        }

        public Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name)
        {
            return GetAppSettings(subscriptionId, resourceGroupName, name, GeoMasterConstants.ProductionSlot);
        }
        public Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name, string slotName = GeoMasterConstants.ProductionSlot)
        {
            return MakeDependencyCall(_geomasterDataProvider.GetAppSettings(subscriptionId, resourceGroupName, name, slotName));
        }

        public Task<dynamic> GetCertificatesInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            return MakeDependencyCall(_observerDataProvider.GetCertificatesInResourceGroupAsync(subscriptionName, resourceGroupName));
        }

        public DataProviderMetadata GetMetadata()
        {
            return _currentMetadataProvider;
        }

        public Task<dynamic> GetResource(string wawsObserverUrl)
        {
            return MakeDependencyCall(_observerDataProvider.GetResource(wawsObserverUrl));
        }

        public Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string siteName)
        {
            return MakeDependencyCall(_observerDataProvider.GetRuntimeSiteSlotMap(siteName));
        }

        public Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName)
        {
            return MakeDependencyCall(_observerDataProvider.GetRuntimeSiteSlotMap(stampName, siteName));
        }

        public Task<dynamic> GetServerFarmsInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            return MakeDependencyCall(_observerDataProvider.GetServerFarmsInResourceGroupAsync(subscriptionName, resourceGroupName));
        }

        public Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm)
        {
            return MakeDependencyCall(_observerDataProvider.GetServerFarmWebspaceName(subscriptionId, serverFarm));
        }

        public Task<dynamic> GetSite(string siteName)
        {
            return MakeDependencyCall(_observerDataProvider.GetSite(siteName));
        }

        public Task<dynamic> GetSite(string stampName, string siteName)
        {
            return MakeDependencyCall(_observerDataProvider.GetSite(stampName, siteName));
        }

        public Task<string> GetStampName(string subscriptionId, string resourceGroupName, string siteName)
        {
            return MakeDependencyCall(_observerDataProvider.GetStampName(subscriptionId, resourceGroupName, siteName));
        }

        public Task<dynamic> GetHostNames(string stampName, string siteName)
        {
            return MakeDependencyCall(_observerDataProvider.GetHostNames(stampName, siteName));
        }

        public Task<dynamic> GetSitePostBody(string stampName, string siteName)
        {
            return MakeDependencyCall(_observerDataProvider.GetSitePostBody(stampName, siteName));
        }

        public Task<dynamic> GetHostingEnvironmentPostBody(string hostingEnvironmentName)
        {
            return MakeDependencyCall(_observerDataProvider.GetHostingEnvironmentPostBody(hostingEnvironmentName));
        }

        public Task<string> GetSiteResourceGroupNameAsync(string siteName)
        {
            return MakeDependencyCall(_observerDataProvider.GetSiteResourceGroupNameAsync(siteName));
        }

        public Task<dynamic> GetSitesInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            return MakeDependencyCall(_observerDataProvider.GetSitesInResourceGroupAsync(subscriptionName, resourceGroupName));
        }

        public Task<dynamic> GetSitesInServerFarmAsync(string subscriptionId, string serverFarmName)
        {
            return MakeDependencyCall(_observerDataProvider.GetSitesInServerFarmAsync(subscriptionId, serverFarmName));
        }

        public Task<string> GetSiteWebSpaceNameAsync(string subscriptionId, string siteName)
        {
            return MakeDependencyCall(_observerDataProvider.GetSiteWebSpaceNameAsync(subscriptionId, siteName));
        }

        public Task<IDictionary<string, string[]>> GetStickySlotSettingNames(string subscriptionId, string resourceGroupName, string name)
        {
            return MakeDependencyCall(_geomasterDataProvider.GetStickySlotSettingNames(subscriptionId, resourceGroupName, name));
        }

        public Task<string> GetStorageVolumeForSiteAsync(string stampName, string siteName)
        {
            return MakeDependencyCall(_observerDataProvider.GetStorageVolumeForSiteAsync(stampName, siteName));
        }

        public Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName)
        {
            return MakeDependencyCall(_observerDataProvider.GetWebspaceResourceGroupName(subscriptionId, webSpaceName));
        }

        public Task<T> MakeHttpGetRequest<T>(string subscriptionId, string resourceGroupName, string name, string slotName, string path = "")
        {
            return MakeDependencyCall(_geomasterDataProvider.MakeHttpGetRequest<T>(subscriptionId, resourceGroupName, name, slotName, path));
        }
        public Task<T> MakeHttpGetRequest<T>(string subscriptionId, string resourceGroupName, string name, string path = "")
        {
            return MakeHttpGetRequest<T>(subscriptionId, resourceGroupName, name, GeoMasterConstants.ProductionSlot, path);
        }

        public Task<T> MakeHttpGetRequestWithFullPath<T>(string fullPath, string queryString = "", string apiVersion = GeoMasterConstants.August2016Version)
        {
            return MakeDependencyCall(_geomasterDataProvider.MakeHttpGetRequestWithFullPath<T>(fullPath, queryString, apiVersion));
        }

        public Task<string> GetLinuxContainerLogs(string subscriptionId, string resourceGroupName, string name, string slotName)
        {
            return MakeDependencyCall(_geomasterDataProvider.GetLinuxContainerLogs(subscriptionId, resourceGroupName, name, slotName));
        }

        public Task<VnetValidationRespone> VerifyHostingEnvironmentVnet(string subscriptionId, string vnetResourceGroup, string vnetName, string vnetSubnetName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return MakeDependencyCall(_geomasterDataProvider.VerifyHostingEnvironmentVnet(subscriptionId, vnetResourceGroup, vnetName, vnetSubnetName, cancellationToken));
        }

        private async Task<T> MakeDependencyCall<T>(Task<T> dataProviderTask, string requestId = null, [CallerMemberName]string dataProviderOperation = "")
        {
            Exception dataProviderException = null;
            DateTime startTime = DateTime.UtcNow, endTime;
            var cancellationTask = Task.FromCanceled(_dataSoureCancellationToken);
            try
            {
                var completedTask = await Task.WhenAny(new Task[] { dataProviderTask, cancellationTask });
                if (completedTask.Id == dataProviderTask.Id)
                {
                    return await dataProviderTask;
                }
                else
                {
                    throw new TimeoutException("DataSource timed out");
                }
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
                    DiagnosticsETWProvider.Instance.LogDataProviderException(requestId ?? _requestId, dataProviderOperation, 
                        startTime.ToString("HH:mm:ss.fff"), endTime.ToString("HH:mm:ss.fff"), 
                        latencyMilliseconds, dataProviderException.GetType().ToString(), dataProviderException.ToString());
                }
                else
                {
                    DiagnosticsETWProvider.Instance.LogDataProviderOperationSummary(requestId ?? _requestId, dataProviderOperation, startTime.ToString("HH:mm:ss.fff"), 
                        endTime.ToString("HH:mm:ss.fff"), latencyMilliseconds);
                }
            }
        }
    }
}
