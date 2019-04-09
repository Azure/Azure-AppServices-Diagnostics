using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Produces("application/json")]
    [Route(UriElements.LogicAppResource)]
    public sealed class LogicAppController : DiagnosticControllerBase<LogicApp>
    {
        public LogicAppController(IStampService stampService, ICompilerHostClient compilerHostClient, ISourceWatcherService sourceWatcherService, IInvokerCacheService invokerCache, IGistCacheService gistCache, IDataSourcesConfigurationService dataSourcesConfigService, IAssemblyCacheService assemblyCacheService, ISearchService searchService)
            : base(stampService, compilerHostClient, sourceWatcherService, invokerCache, gistCache, dataSourcesConfigService, assemblyCacheService, searchService)
        {
        }

        [HttpPost(UriElements.Query)]
        public async Task<IActionResult> ExecuteQuery(string subscriptionId, string resourceGroupName, string logicAppName, [FromBody]CompilationPostBody<dynamic> jsonBody, string startTime = null, string endTime = null, string timeGrain = null, [FromQuery][ModelBinder(typeof(FormModelBinder))] Form Form = null)
        {
            return await base.ExecuteQuery(GetResource(subscriptionId, resourceGroupName, logicAppName), jsonBody, startTime, endTime, timeGrain, Form: Form);
        }

        [HttpPost(UriElements.Detectors)]
        public async Task<IActionResult> ListDetectors(string subscriptionId, string resourceGroupName, string logicAppName, [FromBody] dynamic postBody)
        {
            return await base.ListDetectors(GetResource(subscriptionId, resourceGroupName, logicAppName));
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource)]
        public async Task<IActionResult> GetDetector(string subscriptionId, string resourceGroupName, string logicAppName, string detectorId, [FromBody] dynamic postBody, string startTime = null, string endTime = null, string timeGrain = null, [FromQuery][ModelBinder(typeof(FormModelBinder))] Form form = null)
        {
            return await base.GetDetector(GetResource(subscriptionId, resourceGroupName, logicAppName), detectorId, startTime, endTime, timeGrain, form: form);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.StatisticsQuery)]
        public async Task<IActionResult> ExecuteSystemQuery(string subscriptionId, string resourceGroupName, string logicAppName, [FromBody]CompilationPostBody<dynamic> jsonBody, string detectorId, string dataSource = null, string timeRange = null)
        {
            return await base.ExecuteQuery(GetResource(subscriptionId, resourceGroupName, logicAppName), jsonBody, null, null, null, detectorId, dataSource, timeRange);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.Statistics + UriElements.StatisticsResource)]
        public async Task<IActionResult> GetSystemInvoker(string subscriptionId, string resourceGroupName, string logicAppName, string detectorId, string invokerId, string dataSource = null, string timeRange = null)
        {
            return await base.GetSystemInvoker(GetResource(subscriptionId, resourceGroupName, logicAppName), detectorId, invokerId, dataSource, timeRange);
        }

        [HttpPost(UriElements.Insights)]
        public async Task<IActionResult> GetInsights(string subscriptionId, string resourceGroupName, string logicAppName, [FromBody] dynamic postBody, string pesId, string supportTopicId = null, string startTime = null, string endTime = null, string timeGrain = null)
        {
            return await base.GetInsights(GetResource(subscriptionId, resourceGroupName, logicAppName), pesId, supportTopicId, startTime, endTime, timeGrain);
        }

        /// <summary>
        /// Publish package.
        /// </summary>
        /// <param name="pkg">The package.</param>
        /// <returns>Task for publishing package.</returns>
        [HttpPost(UriElements.Publish)]
        public async Task<IActionResult> PublishPackageAsync([FromBody] Package pkg)
        {
            return await PublishPackage(pkg);
        }

        /// <summary>
        /// List all gists.
        /// </summary>
        /// <returns>Task for listing all gists.</returns>
        [HttpPost(UriElements.Gists)]
        public async Task<IActionResult> ListGistsAsync(string subscriptionId, string resourceGroupName, string logicAppName, [FromBody] dynamic postBody)
        {
            return await base.ListGists(GetResource(subscriptionId, resourceGroupName, logicAppName));
        }

        /// <summary>
        /// List the gist.
        /// </summary>
        /// <param name="subscriptionId">Subscription id.</param>
        /// <param name="resourceGroupName">Resource group name.</param>
        /// <param name="siteName">Site name.</param>
        /// <param name="gistId">Gist id.</param>
        /// <returns>Task for listing the gist.</returns>
        [HttpPost(UriElements.Gists + UriElements.GistResource)]
        public async Task<IActionResult> GetGistAsync(string subscriptionId, string resourceGroupName, string logicAppName, string gistId, [FromBody] dynamic postBody, string startTime = null, string endTime = null, string timeGrain = null)
        {
            return await base.GetDetector(GetResource(subscriptionId, resourceGroupName, logicAppName), gistId, startTime, endTime, timeGrain);
        }
    }
}
