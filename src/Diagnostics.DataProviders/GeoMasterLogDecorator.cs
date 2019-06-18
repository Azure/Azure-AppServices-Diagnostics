using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders
{
	internal class GeoMasterLogDecorator : LogDecoratorBase, IGeoMasterDataProvider
	{
		public IGeoMasterDataProvider DataProvider;

		public GeoMasterLogDecorator(DataProviderContext context, IGeoMasterDataProvider dataProvider) : base(context, dataProvider.GetMetadata())
		{
			DataProvider = dataProvider;
		}

		public Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name)
		{
			return GetAppSettings(subscriptionId, resourceGroupName, name, GeoMasterConstants.ProductionSlot);
		}

		public Task<IDictionary<string, string>> GetAppSettings(string subscriptionId, string resourceGroupName, string name, string slotName = GeoMasterConstants.ProductionSlot)
		{
			return MakeDependencyCall(DataProvider.GetAppSettings(subscriptionId, resourceGroupName, name, slotName));
		}

		public Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name)
		{
			return GetAppDeployments(subscriptionId, resourceGroupName, name, GeoMasterConstants.ProductionSlot);
		}

		public Task<List<IDictionary<string, dynamic>>> GetAppDeployments(string subscriptionId, string resourceGroupName, string name, string slotName)
		{
			return MakeDependencyCall(DataProvider.GetAppDeployments(subscriptionId, resourceGroupName, name, slotName));
		}

		public Task<IDictionary<string, string[]>> GetStickySlotSettingNames(string subscriptionId, string resourceGroupName, string name)
		{
			return MakeDependencyCall(DataProvider.GetStickySlotSettingNames(subscriptionId, resourceGroupName, name));
		}

		public Task<T> MakeHttpGetRequest<T>(string subscriptionId, string resourceGroupName, string name, string slotName, string path = "")
		{
			return MakeDependencyCall(DataProvider.MakeHttpGetRequest<T>(subscriptionId, resourceGroupName, name, slotName, path));
		}

		public Task<T> MakeHttpGetRequest<T>(string subscriptionId, string resourceGroupName, string name, string path = "")
		{
			return MakeHttpGetRequest<T>(subscriptionId, resourceGroupName, name, GeoMasterConstants.ProductionSlot, path);
		}

		public Task<T> MakeHttpGetRequestWithFullPath<T>(string fullPath, string queryString = "", string apiVersion = GeoMasterConstants.August2016Version)
		{
			return MakeDependencyCall(DataProvider.MakeHttpGetRequestWithFullPath<T>(fullPath, queryString, apiVersion));
		}

		public Task<string> GetLinuxContainerLogs(string subscriptionId, string resourceGroupName, string name, string slotName)
		{
			return MakeDependencyCall(DataProvider.GetLinuxContainerLogs(subscriptionId, resourceGroupName, name, slotName));
		}

		public Task<T> InvokeDaasExtension<T>(string subscriptionId, string resourceGroupName, string name, string slotName, string daasApiPath, string apiVersion = GeoMasterConstants.August2016Version, CancellationToken cancellationToken = default(CancellationToken))
		{
			return MakeDependencyCall(DataProvider.InvokeDaasExtension<T>(subscriptionId, resourceGroupName, name, slotName, daasApiPath, apiVersion, cancellationToken));
		}

		public Task<VnetValidationRespone> VerifyHostingEnvironmentVnet(string subscriptionId, string vnetResourceGroup, string vnetName, string vnetSubnetName, CancellationToken cancellationToken = default(CancellationToken))
		{
			return MakeDependencyCall(DataProvider.VerifyHostingEnvironmentVnet(subscriptionId, vnetResourceGroup, vnetName, vnetSubnetName, cancellationToken));
		}
	}
}
