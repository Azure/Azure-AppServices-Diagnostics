using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders
{
    internal class DataProviderLogDecorator : IKustoDataProvider, IGeoMasterDataProvider, ISupportObserverDataProvider
    {
        IKustoDataProvider _kustoDataProvider;
        IGeoMasterDataProvider _geomasterDataProvider;
        ISupportObserverDataProvider _observerDataProvider;

        public DataProviderLogDecorator(IKustoDataProvider dataProvider)
        {
            _kustoDataProvider = dataProvider;
        }

        public DataProviderLogDecorator(IGeoMasterDataProvider dataProvider)
        {
            _geomasterDataProvider = dataProvider;
        }

        public DataProviderLogDecorator(ISupportObserverDataProvider dataProvider)
        {
            _observerDataProvider = dataProvider;
        }

        public async Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, string operationName = null)
        {
            return await MakeDependencyCall(requestId, _kustoDataProvider.ExecuteQuery(query, stampName, requestId, operationName));
        }

        public async Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name)
        {
            return await MakeDependencyCall(null, _geomasterDataProvider.GetAppDeployments(subscriptionId, resourceGroupName, name));
        }

        public async Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name)
        {
            return await MakeDependencyCall(null, _geomasterDataProvider.GetAppSettings(subscriptionId, resourceGroupName, name));
        }

        public async Task<dynamic> GetResource(string wawsObserverUrl)
        {
            return await MakeDependencyCall(null, _observerDataProvider.GetResource(wawsObserverUrl));
        }

        public async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string siteName)
        {
            return await MakeDependencyCall(null, _observerDataProvider.GetRuntimeSiteSlotMap(siteName));
        }

        public async Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName)
        {
            return await MakeDependencyCall(null, _observerDataProvider.GetRuntimeSiteSlotMap(stampName, siteName));
        }

        public async Task<dynamic> GetSite(string siteName)
        {
            return await MakeDependencyCall(null, _observerDataProvider.GetSite(siteName));
        }

        public async Task<dynamic> GetSite(string stampName, string siteName)
        {
            return await MakeDependencyCall(null, _observerDataProvider.GetSite(stampName, siteName));
        }

        public async Task<IDictionary<string, string[]>> GetStickySlotSettingNames(string subscriptionId, string resourceGroupName, string name)
        {
            return await MakeDependencyCall(null, _geomasterDataProvider.GetStickySlotSettingNames(subscriptionId, resourceGroupName, name));
        }

        public async Task<VnetValidationRespone> VerifyHostingEnvironmentVnet(string subscriptionId, string vnetResourceGroup, string vnetName, string vnetSubnetName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await MakeDependencyCall(null, _geomasterDataProvider.VerifyHostingEnvironmentVnet(subscriptionId, vnetResourceGroup, vnetName, vnetSubnetName, cancellationToken));
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
