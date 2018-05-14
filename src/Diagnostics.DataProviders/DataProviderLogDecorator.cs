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
    internal class DataProviderLogDecorator : IKustoDataProvider, IGeoMasterDataProvider, IObserverDataProvider
    {
        IKustoDataProvider _kustoDataProvider;
        IGeoMasterDataProvider _geomasterDataProvider;
        IObserverDataProvider _observerDataProvider;

        public DataProviderLogDecorator(IKustoDataProvider dataProvider)
        {
            _kustoDataProvider = dataProvider;
        }

        public DataProviderLogDecorator(IGeoMasterDataProvider dataProvider)
        {
            _geomasterDataProvider = dataProvider;
        }

        public DataProviderLogDecorator(IObserverDataProvider dataProvider)
        {
            _observerDataProvider = dataProvider;
        }

        public Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, string operationName = null)
        {
            return MakeDependencyCall(requestId, _kustoDataProvider.ExecuteQuery(query, stampName, requestId, operationName));
        }

        public Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name)
        {
            return MakeDependencyCall(null, _geomasterDataProvider.GetAppDeployments(subscriptionId, resourceGroupName, name));
        }

        public Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name)
        {
            return MakeDependencyCall(null, _geomasterDataProvider.GetAppSettings(subscriptionId, resourceGroupName, name));
        }

        public Task<IDictionary<string, string[]>> GetStickySlotSettingNames(string subscriptionId, string resourceGroupName, string name)
        {
            return MakeDependencyCall(null, _geomasterDataProvider.GetStickySlotSettingNames(subscriptionId, resourceGroupName, name));
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
            }catch(Exception ex)
            {
                dataProviderException = ex;
                throw;
            }
            finally
            {
                endTime = DateTime.UtcNow;
                var latencyMilliseconds = Convert.ToInt64((endTime - startTime).TotalMilliseconds);

                if (dataProviderException == null)
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
