using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Produces("application/json")]
    [Route(UriElements.LogicAppResource)]
    public sealed class LogicAppController : DiagnosticControllerBase<LogicApp>
    {
        public LogicAppController(IStampService stampService, ICompilerHostClient compilerHostClient, ISourceWatcherService sourceWatcherService, IInvokerCacheService invokerCache, IDataSourcesConfigurationService dataSourcesConfigService)
            : base(stampService, compilerHostClient, sourceWatcherService, invokerCache, dataSourcesConfigService)
        {
        }

        [HttpPost(UriElements.Query)]
        public async Task<IActionResult> ExecuteQuery(string subscriptionId, string resourceGroupName, string logicAppName, [FromBody]CompilationBostBody<dynamic> jsonBody, string startTime = null, string endTime = null, string timeGrain = null)
        {
            return await base.ExecuteQuery(GetResource(subscriptionId, resourceGroupName, logicAppName), jsonBody, startTime, endTime, timeGrain);
        }

        [HttpPost(UriElements.Detectors)]
        public async Task<IActionResult> ListDetectors(string subscriptionId, string resourceGroupName, string logicAppName, [FromBody] dynamic postBody)
        {
            return await base.ListDetectors(GetResource(subscriptionId, resourceGroupName, logicAppName));
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource)]
        public async Task<IActionResult> GetDetector(string subscriptionId, string resourceGroupName, string logicAppName, string detectorId, [FromBody] dynamic postBody, string startTime = null, string endTime = null, string timeGrain = null)
        {
            return await base.GetDetector(GetResource(subscriptionId, resourceGroupName, logicAppName), detectorId, startTime, endTime, timeGrain);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.StatisticsQuery)]
        public async Task<IActionResult> ExecuteSystemQuery(string subscriptionId, string resourceGroupName, string logicAppName, [FromBody]CompilationBostBody<dynamic> jsonBody, string detectorId, string dataSource = null, string timeRange = null)
        {
            return await base.ExecuteQuery(GetResource(subscriptionId, resourceGroupName, logicAppName), jsonBody, null, null, null, detectorId, dataSource, timeRange);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.Statistics + UriElements.StatisticsResource)]
        public async Task<IActionResult> GetSystemInvoker(string subscriptionId, string resourceGroupName, string logicAppName, string detectorId, string invokerId, string dataSource = null, string timeRange = null)
        {
            return await base.GetSystemInvoker(detectorId, invokerId, dataSource, timeRange);
        }

        [HttpPost(UriElements.Insights)]
        public async Task<IActionResult> GetInsights(string subscriptionId, string resourceGroupName, string logicAppName, [FromBody] dynamic postBody, string supportTopicId, string minimumSeverity = null, string startTime = null, string endTime = null, string timeGrain = null)
        {
            return await base.GetInsights(GetResource(subscriptionId, resourceGroupName, logicAppName), supportTopicId, minimumSeverity, startTime, endTime, timeGrain);
        }
    }
}
