using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json.Linq;

namespace Diagnostics.DataProviders
{
	internal class ObserverLogDecorator : LogDecoratorBase, ISupportObserverDataProvider
	{
		public ISupportObserverDataProvider DataProvider;

		public ObserverLogDecorator(DataProviderContext context, ISupportObserverDataProvider dataProvider) : base((DiagnosticDataProvider)dataProvider, context, dataProvider.GetMetadata())
		{
			DataProvider = dataProvider;
		}
                                
		public Task<dynamic> GetCertificatesInResourceGroupAsync(string subscriptionName, string resourceGroupName)
		{
			return MakeDependencyCall(DataProvider.GetCertificatesInResourceGroupAsync(subscriptionName, resourceGroupName));
		}

		public Task<dynamic> GetResource(string wawsObserverUrl)
		{
			return MakeDependencyCall(DataProvider.GetResource(wawsObserverUrl));
		}

		public Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName)
		{
			return MakeDependencyCall(DataProvider.GetRuntimeSiteSlotMap(stampName, siteName));
		}

		public Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName, string slotName)
		{
			return MakeDependencyCall(DataProvider.GetRuntimeSiteSlotMap(stampName, siteName, slotName));
		}

        public Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(OperationContext<App> cxt, string stampName = null, string siteName = null, string slotName = null, DateTime? endTime = null)
        {
            return MakeDependencyCall(DataProvider.GetRuntimeSiteSlotMap(cxt, stampName, siteName, slotName, endTime));
        }

        public Task<dynamic> GetServerFarmsInResourceGroupAsync(string subscriptionName, string resourceGroupName)
		{
			return MakeDependencyCall(DataProvider.GetServerFarmsInResourceGroupAsync(subscriptionName, resourceGroupName));
		}

		public Task<string> GetServerFarmWebspaceName(string subscriptionId, string serverFarm)
		{
			return MakeDependencyCall(DataProvider.GetServerFarmWebspaceName(subscriptionId, serverFarm));
		}

		public Task<dynamic> GetSite(string siteName)
		{
			return MakeDependencyCall(DataProvider.GetSite(siteName));
		}

		public Task<dynamic> GetSite(string stampName, string siteName)
		{
			return MakeDependencyCall(DataProvider.GetSite(stampName, siteName));
		}

		public Task<dynamic> GetSite(string stampName, string siteName, string slotName)
		{
			return MakeDependencyCall(DataProvider.GetSite(stampName, siteName, slotName));
		}

		public Task<string> GetStampName(string subscriptionId, string resourceGroupName, string siteName)
		{
			return MakeDependencyCall(DataProvider.GetStampName(subscriptionId, resourceGroupName, siteName));
		}

		public Task<dynamic> GetHostNames(string stampName, string siteName)
		{
			return MakeDependencyCall(DataProvider.GetHostNames(stampName, siteName));
		}

		public Task<dynamic> GetSitePostBody(string stampName, string siteName)
		{
			return MakeDependencyCall(DataProvider.GetSitePostBody(stampName, siteName));
		}

		public Task<dynamic> GetHostingEnvironmentPostBody(string hostingEnvironmentName)
		{
			return MakeDependencyCall(DataProvider.GetHostingEnvironmentPostBody(hostingEnvironmentName));
		}

		public Task<string> GetSiteResourceGroupNameAsync(string siteName)
		{
			return MakeDependencyCall(DataProvider.GetSiteResourceGroupNameAsync(siteName));
		}

		public Task<dynamic> GetSitesInResourceGroupAsync(string subscriptionName, string resourceGroupName)
		{
			return MakeDependencyCall(DataProvider.GetSitesInResourceGroupAsync(subscriptionName, resourceGroupName));
		}

		public Task<dynamic> GetSitesInServerFarmAsync(string subscriptionId, string serverFarmName)
		{
			return MakeDependencyCall(DataProvider.GetSitesInServerFarmAsync(subscriptionId, serverFarmName));
		}

		public Task<string> GetSiteWebSpaceNameAsync(string subscriptionId, string siteName)
		{
			return MakeDependencyCall(DataProvider.GetSiteWebSpaceNameAsync(subscriptionId, siteName));
		}

		public Task<string> GetWebspaceResourceGroupName(string subscriptionId, string webSpaceName)
		{
			return MakeDependencyCall(DataProvider.GetWebspaceResourceGroupName(subscriptionId, webSpaceName));
		}

		public Task<DataTable> ExecuteSqlQueryAsync(string cloudServiceName, string query)
		{
			return MakeDependencyCall(DataProvider.ExecuteSqlQueryAsync(cloudServiceName, query));
		}

        public Task<JArray> GetAdminSitesAsync(string siteName)
        {
            return MakeDependencyCall(DataProvider.GetAdminSitesAsync(siteName));
        }

        public Task<JArray> GetAdminSitesAsync(string siteName, string stampName)
        {
            return MakeDependencyCall(DataProvider.GetAdminSitesAsync(siteName, stampName));
        }
    }
}
