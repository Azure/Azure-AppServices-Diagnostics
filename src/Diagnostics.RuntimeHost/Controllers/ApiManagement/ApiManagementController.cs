using System;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Configuration;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Authorize]
    [Produces("application/json")]
    [Route(UriElements.ApiManagementServiceResource)]
    public sealed class ApiManagementController : DiagnosticControllerBase<ApiManagementService>
    {
        public ApiManagementController(IServiceProvider services, IRuntimeContext<ApiManagementService> runtimeContext, IConfiguration config)
            : base(services, runtimeContext, config)
        {
        }

        [HttpPost(UriElements.Query)]
        public async Task<IActionResult> ExecuteQuery(string subscriptionId, string resourceGroupName, string serviceName, [FromBody]CompilationPostBody<dynamic> jsonBody, string startTime = null, string endTime = null, string timeGrain = null, [FromQuery][ModelBinder(typeof(FormModelBinder))] Form Form = null)
        {
            return await base.ExecuteQuery(GetResource(subscriptionId, resourceGroupName, serviceName), jsonBody, startTime, endTime, timeGrain, Form: Form);
        }

        [HttpPost(UriElements.Detectors)]
        public async Task<IActionResult> ListDetectors(string subscriptionId, string resourceGroupName, string serviceName, [FromBody] dynamic postBody, [FromQuery(Name = "text")] string text = null, [FromQuery] string l = "")
        {
            return await base.ListDetectors(GetResource(subscriptionId, resourceGroupName, serviceName), text, language: l.ToLower());
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource)]
        public async Task<IActionResult> GetDetector(string subscriptionId, string resourceGroupName, string serviceName, string detectorId, [FromBody] dynamic postBody, string startTime = null, string endTime = null, string timeGrain = null, [FromQuery][ModelBinder(typeof(FormModelBinder))] Form form = null, [FromQuery] string l = "")
        {
            return await base.GetDetector(GetResource(subscriptionId, resourceGroupName, serviceName), detectorId, startTime, endTime, timeGrain, form: form, language: l.ToLower());
        }

        [HttpPost(UriElements.DiagnosticReport)]
        public async Task<IActionResult> DiagnosticReport(string subscriptionId, string resourceGroupName, string serviceName, [FromBody] DiagnosticReportQuery queryBody, string startTime = null, string endTime = null, string timeGrain = null)
        {
            var validateBody = InsightsAPIHelpers.ValidateQueryBody(queryBody);
            if (!validateBody.Status)
            {
                return BadRequest($"Invalid post body. {validateBody.Message}");
            }
            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }
            return await base.GetDiagnosticReport(GetResource(subscriptionId, resourceGroupName, serviceName), queryBody, startTimeUtc, endTimeUtc, timeGrainTimeSpan);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.StatisticsQuery)]
        public async Task<IActionResult> ExecuteSystemQuery(string subscriptionId, string resourceGroupName, string serviceName, [FromBody]CompilationPostBody<dynamic> jsonBody, string detectorId, string dataSource = null, string timeRange = null)
        {
            return await base.ExecuteQuery(GetResource(subscriptionId, resourceGroupName, serviceName), jsonBody, null, null, null, detectorId, dataSource, timeRange);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.Statistics + UriElements.StatisticsResource)]
        public async Task<IActionResult> GetSystemInvoker(string subscriptionId, string resourceGroupName, string serviceName, string detectorId, string invokerId, string dataSource = null, string timeRange = null)
        {
            return await base.GetSystemInvoker(GetResource(subscriptionId, resourceGroupName, serviceName), detectorId, invokerId, dataSource, timeRange);
        }

        [HttpPost(UriElements.Insights)]
        public async Task<IActionResult> GetInsights(string subscriptionId, string resourceGroupName, string serviceName, [FromBody] dynamic postBody, string pesId, string supportTopicId = null, string supportTopic = null, string startTime = null, string endTime = null, string timeGrain = null)
        {
            string postBodyString;
            try
            {
                postBodyString = JsonConvert.SerializeObject(postBody.Parameters);
            }
            catch (RuntimeBinderException)
            {
                postBodyString = "";
            }
            return await base.GetInsights(GetResource(subscriptionId, resourceGroupName, serviceName), pesId, supportTopicId, startTime, endTime, timeGrain, supportTopic, postBodyString);
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
        public async Task<IActionResult> ListGistsAsync(string subscriptionId, string resourceGroupName, string serviceName, [FromBody] dynamic postBody)
        {
            return await base.ListGists(GetResource(subscriptionId, resourceGroupName, serviceName));
        }

        /// <summary>
        /// List the gist.
        /// </summary>
        /// <param name="subscriptionId">Subscription id.</param>
        /// <param name="resourceGroupName">Resource group name.</param>
        /// <param name="gistId">Gist id.</param>
        /// <returns>Task for listing the gist.</returns>
        [HttpPost(UriElements.Gists + UriElements.GistResource)]
        public async Task<IActionResult> GetGistAsync(string subscriptionId, string resourceGroupName, string serviceName, string gistId, [FromBody] dynamic postBody, string startTime = null, string endTime = null, string timeGrain = null)
        {
            return await base.GetGist(GetResource(subscriptionId, resourceGroupName, serviceName), gistId, startTime, endTime, timeGrain);
        }
    }
}
