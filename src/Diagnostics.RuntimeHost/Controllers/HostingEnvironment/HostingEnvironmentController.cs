using Diagnostics.ModelsAndUtils.Models;
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
    [Route(UriElements.HostingEnvironmentResource)]
    public sealed class HostingEnvironmentController : DiagnosticControllerBase<HostingEnvironment>
    {
        public HostingEnvironmentController(IStampService stampService, ICompilerHostClient compilerHostClient, ISourceWatcherService sourceWatcherService, IInvokerCacheService invokerCache, IDataSourcesConfigurationService dataSourcesConfigService)
            : base(stampService, compilerHostClient, sourceWatcherService, invokerCache, dataSourcesConfigService)
        {
        }

        [HttpPost(UriElements.Query)]
        public async Task<IActionResult> ExecuteQuery(string subscriptionId, string resourceGroupName, string hostingEnvironmentName, string[] hostNames, string stampName, [FromBody]CompilationBostBody<DiagnosticStampData> jsonBody, string startTime = null, string endTime = null, string timeGrain = null)
        {
            if (jsonBody == null)
            {
                return BadRequest("Missing body");
            }
            
            if (jsonBody.Resource == null)
            {
                return BadRequest("Missing Hosting Enviroment Data in body");
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            HostingEnvironment ase = await GetHostingEnvironment(subscriptionId, resourceGroupName, hostingEnvironmentName, jsonBody.Resource, startTimeUtc, endTimeUtc);
            return await base.ExecuteQuery(ase, jsonBody, startTime, endTime, timeGrain);
        }

        [HttpPost(UriElements.Detectors)]
        public async Task<IActionResult> ListDetectors(string subscriptionId, string resourceGroupName, string hostingEnvironmentName, [FromBody] DiagnosticStampData postBody)
        {
            if (postBody == null)
            {
                return BadRequest("Post Body missing.");
            }

            DateTimeHelper.PrepareStartEndTimeWithTimeGrain(string.Empty, string.Empty, string.Empty, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage);
            HostingEnvironment ase = await GetHostingEnvironment(subscriptionId, resourceGroupName, hostingEnvironmentName, postBody, startTimeUtc, endTimeUtc);
            return await base.ListDetectors(ase);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource)]
        public async Task<IActionResult> GetDetector(string subscriptionId, string resourceGroupName, string hostingEnvironmentName, string detectorId, [FromBody] DiagnosticStampData postBody, string startTime = null, string endTime = null, string timeGrain = null)
        {
            if (postBody == null)
            {
                return BadRequest("Post Body missing.");
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            HostingEnvironment ase = await GetHostingEnvironment(subscriptionId, resourceGroupName, hostingEnvironmentName, postBody, startTimeUtc, endTimeUtc);
            return await base.GetDetector(ase, detectorId, startTime, endTime, timeGrain);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.StatisticsQuery)]
        public async Task<IActionResult> ExecuteSystemQuery(string subscriptionId, string resourceGroupName, string hostingEnvironmentName, string[] hostNames, string stampName, [FromBody]CompilationBostBody<DiagnosticStampData> jsonBody, string detectorId, string startTime = null, string endTime = null, string timeGrain = null)
        {
            HostingEnvironment ase = new HostingEnvironment(subscriptionId, resourceGroupName, hostingEnvironmentName);
            return await base.ExecuteQuery(ase, jsonBody, startTime, endTime, timeGrain, detectorId);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.Statistics + UriElements.StatisticsResource)]
        public async Task<IActionResult> GetSystemInvoker(string detectorId, string invokerId)
        {
            return await base.GetSystemInvoker(detectorId, invokerId);
        }

        [HttpPost(UriElements.Insights)]
        public async Task<IActionResult> GetInsights(string subscriptionId, string resourceGroupName, string hostingEnvironmentName, [FromBody] DiagnosticStampData postBody, string supportTopicId, string minimumSeverity = null, string startTime = null, string endTime = null, string timeGrain = null)
        {
            if (postBody == null)
            {
                return BadRequest("Post Body missing.");
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            HostingEnvironment ase = await GetHostingEnvironment(subscriptionId, resourceGroupName, hostingEnvironmentName, postBody, startTimeUtc, endTimeUtc);
            return await base.GetInsights(ase, supportTopicId, minimumSeverity, startTime, endTime, timeGrain);
        }
    }
}
