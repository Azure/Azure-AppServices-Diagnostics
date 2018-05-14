using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            return _kustoDataProvider.ExecuteQuery(query, stampName, requestId, operationName);
        }

        public Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name)
        {
            return _geomasterDataProvider.GetAppDeployments(subscriptionId, resourceGroupName, name);
        }

        public Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name)
        {
            return _geomasterDataProvider.GetAppSettings(subscriptionId, resourceGroupName, name);
        }

        public Task<IDictionary<string, string[]>> GetStickySlotSettingNames(string subscriptionId, string resourceGroupName, string name)
        {
            return _geomasterDataProvider.GetStickySlotSettingNames(subscriptionId, resourceGroupName, name);
        }

        public Task<VnetValidationRespone> VerifyHostingEnvironmentVnet(string subscriptionId, string vnetResourceGroup, string vnetName, string vnetSubnetName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _geomasterDataProvider.VerifyHostingEnvironmentVnet(subscriptionId, vnetResourceGroup, vnetName, vnetSubnetName, cancellationToken);
        }

        private Task<T> MakeDependencyCall<T>(string dataProviderOperation, Task<T> dataProviderTask)
        {
            Exception dataProviderException = null;
            try
            {

            }catch(Exception ex)
            {
                dataProviderException = ex;
            }
            finally
            {
                
                if (dataProviderException == null)
                {

                }
                else
                {

                }
            }

            return null;
        }

        private Task<T> Run<T>(Func<object, T> dataProvider)
        {
            return null;
        }
    }
}
