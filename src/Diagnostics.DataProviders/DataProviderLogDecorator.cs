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
    internal class DataProviderLogDecorator : IKustoDataProvider, IGeoMasterDataProvider, ISupportObserverDataProvider, IAppInsightsDataProvider, IMdmDataProvider
    {
        private IKustoDataProvider _kustoDataProvider;
        private IGeoMasterDataProvider _geomasterDataProvider;
        private ISupportObserverDataProvider _observerDataProvider;
        private IAppInsightsDataProvider _appInsightsDataProvider;
        private IMdmDataProvider _mdmDataProvider;
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

        public DataProviderLogDecorator(DataProviderContext context, IMdmDataProvider dataProvider) : this(context, dataProvider.GetMetadata())
        {
            _mdmDataProvider = dataProvider;
        }

        private DataProviderLogDecorator(DataProviderContext context, DataProviderMetadata metaData)
        {
            _currentMetadataProvider = metaData;
            _requestId = context.RequestId;
            _dataSoureCancellationToken = context.DataSourcesCancellationToken;
        }

        public DataProviderMetadata GetMetadata()
        {
            return _currentMetadataProvider;
        }

        #region AppInsight_DataProvider

        public Task<DataTable> ExecuteAppInsightsQuery(string query)
        {
            return MakeDependencyCall(_appInsightsDataProvider.ExecuteAppInsightsQuery(query));
        }

        public Task<bool> SetAppInsightsKey(string appId, string apiKey)
        {
            return MakeDependencyCall(_appInsightsDataProvider.SetAppInsightsKey(appId, apiKey));
        }

        #endregion

        #region Kusto_DataProvider

        public Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, string operationName = null)
        {
            return MakeDependencyCall(_kustoDataProvider.ExecuteQuery(query, stampName, _requestId, operationName));
        }

        public Task<DataTable> ExecuteClusterQuery(string query, string requestId = null, string operationName = null)
        {
            return ExecuteQuery(query, DataProviderConstants.FakeStampForAnalyticsCluster, requestId, operationName);
        }

        public Task<KustoQuery> GetKustoQuery(string query, string stampName)
        {
            return MakeDependencyCall(_kustoDataProvider.GetKustoQuery(query, stampName));
        }
        public Task<KustoQuery> GetKustoClusterQuery(string query)
        {
            return MakeDependencyCall(_kustoDataProvider.GetKustoClusterQuery(query));
        }

        #endregion

        #region Observer_DataProvider

        public Task<JObject> GetAdminSitesByHostNameAsync(string stampName, string[] hostNames)
        {
            return MakeDependencyCall(_observerDataProvider.GetAdminSitesByHostNameAsync(stampName, hostNames));
        }

        public Task<JObject> GetAdminSitesBySiteNameAsync(string stampName, string siteName)
        {
            return MakeDependencyCall(_observerDataProvider.GetAdminSitesBySiteNameAsync(stampName, siteName));
        }

        public Task<IEnumerable<object>> GetAppServiceEnvironmentDeploymentsAsync(string hostingEnvironmentName)
        {
            return MakeDependencyCall(_observerDataProvider.GetAppServiceEnvironmentDeploymentsAsync(hostingEnvironmentName));
        }

        public Task<JObject> GetAppServiceEnvironmentDetailsAsync(string hostingEnvironmentName)
        {
            return MakeDependencyCall(_observerDataProvider.GetAppServiceEnvironmentDetailsAsync(hostingEnvironmentName));
        }

        public Task<dynamic> GetCertificatesInResourceGroupAsync(string subscriptionName, string resourceGroupName)
        {
            return MakeDependencyCall(_observerDataProvider.GetCertificatesInResourceGroupAsync(subscriptionName, resourceGroupName));
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

        public Task<dynamic> GetSite(string stampName, string siteName, string slotName)
        {
            return MakeDependencyCall(_observerDataProvider.GetSite(stampName, siteName, slotName));
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

        public Task<string> GetStorageVolumeForSiteAsync(string stampName, string siteName)
        {
            return MakeDependencyCall(_observerDataProvider.GetStorageVolumeForSiteAsync(stampName, siteName));
        }

        public Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName)
        {
            return MakeDependencyCall(_observerDataProvider.GetWebspaceResourceGroupName(subscriptionId, webSpaceName));
        }

        #endregion

        #region GeoMaster_DataProvider

        public Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name)
        {
            return GetAppSettings(subscriptionId, resourceGroupName, name, GeoMasterConstants.ProductionSlot);
        }

        public Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name, string slotName = GeoMasterConstants.ProductionSlot)
        {
            return MakeDependencyCall(_geomasterDataProvider.GetAppSettings(subscriptionId, resourceGroupName, name, slotName));
        }

        public Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name)
        {
            return GetAppDeployments(subscriptionId, resourceGroupName, name, GeoMasterConstants.ProductionSlot);
        }

        public Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name, string slotName)
        {
            return MakeDependencyCall(_geomasterDataProvider.GetAppDeployments(subscriptionId, resourceGroupName, name, slotName));
        }

        public Task<IDictionary<string, string[]>> GetStickySlotSettingNames(string subscriptionId, string resourceGroupName, string name)
        {
            return MakeDependencyCall(_geomasterDataProvider.GetStickySlotSettingNames(subscriptionId, resourceGroupName, name));
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

        public Task<T> InvokeDaasExtension<T>(string subscriptionId, string resourceGroupName, string name, string slotName, string daasApiPath, string apiVersion = GeoMasterConstants.August2016Version, CancellationToken cancellationToken = default(CancellationToken))
        {
            return MakeDependencyCall(_geomasterDataProvider.InvokeDaasExtension<T>(subscriptionId, resourceGroupName, name, slotName, daasApiPath, apiVersion, cancellationToken));
        }

        public Task<VnetValidationRespone> VerifyHostingEnvironmentVnet(string subscriptionId, string vnetResourceGroup, string vnetName, string vnetSubnetName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return MakeDependencyCall(_geomasterDataProvider.VerifyHostingEnvironmentVnet(subscriptionId, vnetResourceGroup, vnetName, vnetSubnetName, cancellationToken));
        }

        #endregion

        #region MDM_DataProvider

        /// <summary>
        /// Gets the list of namespaces for the monitoringAccount.
        /// </summary>
        /// <returns>The list of namespaces for the monitoringAccount.</returns>
        public Task<IEnumerable<string>> GetNamespacesAsync()
        {
            return MakeDependencyCall(_mdmDataProvider.GetNamespacesAsync());
        }

        /// <summary>
        /// Gets the list of metric names for the monitoringAccount and metricNamespace.
        /// </summary>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <returns>The list of metric names for the monitoringAccount and metricNamespace.</returns>
        public Task<IEnumerable<string>> GetMetricNamesAsync(string metricNamespace)
        {
            return MakeDependencyCall(_mdmDataProvider.GetMetricNamesAsync(metricNamespace));
        }

        /// <summary>
        /// Gets the list of dimension names for the metricId.
        /// </summary>
        /// <param name="metricNamespace">Metric namespace</param>
        /// <param name="metricName">Metric name</param>
        /// <returns>The list of dimension names for the metricId.</returns>
        public Task<IEnumerable<string>> GetDimensionNamesAsync(string metricNamespace, string metricName)
        {
            return MakeDependencyCall(_mdmDataProvider.GetDimensionNamesAsync(metricNamespace, metricName));
        }

        /// <summary>
        /// Gets the dimension values for dimensionName satifying the dimensionFilters and
        /// </summary>
        /// <param name="metricNamespace">Metric namespace</param>
        /// <param name="metricName">Metric name</param>
        /// <param name="filter">The dimension filters representing the pre-aggregate dimensions. Create an emtpy include filter for dimension with no filter values. Requested dimension should also be part of this and should be empty.</param>
        /// <param name="dimensionName">Name of the dimension for which values are requested.</param>
        /// <param name="startTimeUtc">Start time for evaluating dimension values.</param>
        /// <param name="endTimeUtc">End time for evaluating dimension values.</param>
        /// <returns>Dimension values for dimensionName.</returns>
        public Task<IEnumerable<string>> GetDimensionValuesAsync(string metricNamespace, string metricName, List<Tuple<string, IEnumerable<string>>> filter, string dimensionName, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            return MakeDependencyCall(_mdmDataProvider.GetDimensionValuesAsync(metricNamespace, metricName, filter, dimensionName, startTimeUtc, endTimeUtc));
        }

        /// <summary>
        /// Gets the dimension values for dimensionName satifying the dimensionFilters and
        /// </summary>
        /// <param name="metricNamespace">Metric namespace</param>
        /// <param name="metricName">Metric name</param>
        /// <param name="dimensionName">Name of the dimension for which values are requested.</param>
        /// <param name="startTimeUtc">Start time for evaluating dimension values.</param>
        /// <param name="endTimeUtc">End time for evaluating dimension values.</param>
        /// <returns>Dimension values for dimensionName.</returns>
        public Task<IEnumerable<string>> GetDimensionValuesAsync(string metricNamespace, string metricName, string dimensionName, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            return MakeDependencyCall(_mdmDataProvider.GetDimensionValuesAsync(metricNamespace, metricName, dimensionName, startTimeUtc, endTimeUtc));
        }

        /// <summary>
        /// Gets the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="sampling">The sampling type.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">The metric name.</param>
        /// <param name="dimension">The dimension.</param>
        /// <returns>The time series for the given definition.</returns>
        public Task<IEnumerable<DataTable>> GetTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, Sampling sampling, string metricNamespace, string metricName, IDictionary<string, string> dimension)
        {
            return MakeDependencyCall(_mdmDataProvider.GetTimeSeriesAsync(startTimeUtc, endTimeUtc, sampling, metricNamespace, metricName, dimension));
        }

        /// <summary>
        /// Gets a list of the time series.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="sampling">The sampling type.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <returns>The time series of for the given definitions.</returns>
        public Task<IEnumerable<DataTable>> GetMultipleTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, Sampling sampling, int seriesResolutionInMinutes, IEnumerable<Tuple<string, string, IEnumerable<KeyValuePair<string, string>>>> definitions)
        {
            return MakeDependencyCall(_mdmDataProvider.GetMultipleTimeSeriesAsync(startTimeUtc, endTimeUtc, sampling, seriesResolutionInMinutes, definitions));
        }

        /// <summary>
        /// Gets a list of the time series, each with multiple sampling types.
        /// </summary>
        /// <param name="startTimeUtc">The start time UTC.</param>
        /// <param name="endTimeUtc">The end time UTC.</param>
        /// <param name="sampling">The sampling types.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <param name="seriesResolutionInMinutes">The resolution window used to reduce the resolution of the returned series.</param>
        /// <param name="aggregationType">The aggregation function used to reduce the resolution of the returned series.</param>
        /// <returns>The time series of for the given definitions.</returns>
        public Task<IEnumerable<DataTable>> GetMultipleTimeSeriesAsync(DateTime startTimeUtc, DateTime endTimeUtc, Sampling sampling, IEnumerable<Tuple<string, string, IEnumerable<KeyValuePair<string, string>>>> definitions, int seriesResolutionInMinutes = 1, AggregationType aggregationType = AggregationType.Automatic)
        {
            return MakeDependencyCall(_mdmDataProvider.GetMultipleTimeSeriesAsync(startTimeUtc, endTimeUtc, sampling, definitions, seriesResolutionInMinutes, aggregationType));
        }

        #endregion

        private async Task<T> MakeDependencyCall<T>(Task<T> dataProviderTask, [CallerMemberName]string dataProviderOperation = "")
        {
            Exception dataProviderException = null;
            DateTime startTime = DateTime.UtcNow, endTime;
            CancellationTokenRegistration cTokenRegistration;

            try
            {
                var tcs = new TaskCompletionSource<bool>();
                cTokenRegistration = _dataSoureCancellationToken.Register(() => tcs.TrySetResult(true));
                var completedTask = await Task.WhenAny(new Task[] { dataProviderTask, tcs.Task });

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
                if (cTokenRegistration != null)
                {
                    cTokenRegistration.Dispose();
                }

                endTime = DateTime.UtcNow;
                var latencyMilliseconds = Convert.ToInt64((endTime - startTime).TotalMilliseconds);

                if (dataProviderException != null)
                {
                    DiagnosticsETWProvider.Instance.LogDataProviderException(_requestId, dataProviderOperation, 
                        startTime.ToString("HH:mm:ss.fff"), endTime.ToString("HH:mm:ss.fff"), 
                        latencyMilliseconds, dataProviderException.GetType().ToString(), dataProviderException.ToString());
                }
                else
                {
                    DiagnosticsETWProvider.Instance.LogDataProviderOperationSummary(_requestId, dataProviderOperation, startTime.ToString("HH:mm:ss.fff"), 
                        endTime.ToString("HH:mm:ss.fff"), latencyMilliseconds);
                }
            }
        }
    }
}
