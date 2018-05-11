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
            throw new NotImplementedException();
        }

        public Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name)
        {
            throw new NotImplementedException();
        }

        public Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name)
        {
            throw new NotImplementedException();
        }

        public Task<IDictionary<string, string[]>> GetStickySlotSettingNames(string subscriptionId, string resourceGroupName, string name)
        {
            throw new NotImplementedException();
        }

        public Task<VnetValidationRespone> VerifyHostingEnvironmentVnet(string subscriptionId, string vnetResourceGroup, string vnetName, string vnetSubnetName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        private Task<T> Run<T>(object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            return null;
        }
    }
}
