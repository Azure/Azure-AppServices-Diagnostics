using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.GeoMaster;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Produces("application/json")]
    [Route(UriElements.SitesResource + UriElements.Diagnostics)]
    public sealed class SitesController : SiteControllerBase
    {
        public SitesController(IStampService stampService, ISiteService siteService, ICompilerHostClient compilerHostClient, ISourceWatcherService sourceWatcherService, IInvokerCacheService invokerCache, IDataSourcesConfigurationService dataSourcesConfigService)
            : base(stampService, siteService, compilerHostClient, sourceWatcherService, invokerCache, dataSourcesConfigService)
        {
        }

        [HttpPost(UriElements.Query)]
        public async Task<IActionResult> Post(string subscriptionId, string resourceGroupName, string siteName, [FromBody]CompilationBostBody<DiagnosticSiteData> jsonBody, string startTime = null, string endTime = null, string timeGrain = null)
        {
            if (jsonBody == null)
            {
                return BadRequest("Missing body");
            }
            
            if (jsonBody.Resource == null)
            {
                return BadRequest("Missing Site data in body");
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            App app = await GetAppResource(subscriptionId, resourceGroupName, siteName, jsonBody.Resource, startTimeUtc, endTimeUtc);
            return await base.ExecuteQuery(app, jsonBody, startTime, endTime, timeGrain);
        }

        [HttpPost(UriElements.Detectors)]
        public async Task<IActionResult> ListDetectors(string subscriptionId, string resourceGroupName, string siteName, [FromBody] DiagnosticSiteData postBody)
        {
            if (postBody == null)
            {
                return BadRequest("Post Body missing.");
            }

            DateTimeHelper.PrepareStartEndTimeWithTimeGrain(string.Empty, string.Empty, string.Empty, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage);
            App app = await GetAppResource(subscriptionId, resourceGroupName, siteName, postBody, startTimeUtc, endTimeUtc);
            return await base.ListDetectors(app);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource)]
        public async Task<IActionResult> GetDetector(string subscriptionId, string resourceGroupName, string siteName, string detectorId, [FromBody] DiagnosticSiteData postBody, string startTime = null, string endTime = null, string timeGrain = null)
        {
            if (postBody == null)
            {
                return BadRequest("Post Body missing.");
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            App app = await GetAppResource(subscriptionId, resourceGroupName, siteName, postBody, startTimeUtc, endTimeUtc);
            return await base.GetDetector(app, detectorId, startTime, endTime, timeGrain);
        }

        [HttpPost(UriElements.Insights)]
        public async Task<IActionResult> GetInsights(string subscriptionId, string resourceGroupName, string siteName, [FromBody] DiagnosticSiteData postBody, string supportTopicId, string minimumSeverity = null, string startTime = null, string endTime = null, string timeGrain = null)
        {
            if (postBody == null)
            {
                return BadRequest("Post Body missing.");
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            App app = await GetAppResource(subscriptionId, resourceGroupName, siteName, postBody, startTimeUtc, endTimeUtc);
            return await base.GetInsights(app, supportTopicId, minimumSeverity, startTime, endTime, timeGrain);
        }
    }
}