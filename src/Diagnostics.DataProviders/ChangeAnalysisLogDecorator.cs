using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models.ChangeAnalysis;

namespace Diagnostics.DataProviders
{
    internal class ChangeAnalysisLogDecorator : LogDecoratorBase, IChangeAnalysisDataProvider
    {
        public IChangeAnalysisDataProvider DataProvider;

        public ChangeAnalysisLogDecorator(DataProviderContext context, IChangeAnalysisDataProvider dataProvider) : base(context, dataProvider.GetMetadata())
        {
            DataProvider = dataProvider;
        }

        public Task<ResourceIdResponseModel> GetDependentResourcesAsync(string sitename, string subscriptionId, string stamp, string startTime, string endTime)
        {
            return MakeDependencyCall(DataProvider.GetDependentResourcesAsync(sitename, subscriptionId, stamp, startTime, endTime));
        }

        public Task<List<ChangeSetResponseModel>> GetChangeSetsForResource(string armResourceUri, DateTime startTime, DateTime endTime)
        {
            return MakeDependencyCall(DataProvider.GetChangeSetsForResource(armResourceUri, startTime, endTime));
        }

        public Task<List<ResourceChangesResponseModel>> GetChangesByChangeSetId(string changeSetId, string resourceUri)
        {
            return MakeDependencyCall(DataProvider.GetChangesByChangeSetId(changeSetId, resourceUri));
        }

        public Task<LastScanResponseModel> GetLastScanInformation(string armResourceUri)
        {
            return MakeDependencyCall(DataProvider.GetLastScanInformation(armResourceUri));
        }

        public Task<SubscriptionOnboardingStatus> GetSubscriptionOnboardingStatus(string subscriptionId)
        {
            return MakeDependencyCall(DataProvider.GetSubscriptionOnboardingStatus(subscriptionId));
        }

        public Task<ChangeScanModel> ScanActionRequest(string resourceId, string scanAction)
        {
            return MakeDependencyCall(DataProvider.ScanActionRequest(resourceId, scanAction));
        }

        public Task<string> InvokeChangeAnalysisRequest(string requestUri, object postBody = null, HttpMethod method = null)
        {
            return MakeDependencyCall(DataProvider.InvokeChangeAnalysisRequest(requestUri, postBody, method));
        }
    }
}
