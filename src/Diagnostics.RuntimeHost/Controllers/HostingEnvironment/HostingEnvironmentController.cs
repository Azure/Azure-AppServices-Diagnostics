using System;
using System.Threading.Tasks;
using Diagnostics.DataProviders;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Produces("application/json")]
    [Route(UriElements.HostingEnvironmentResource)]
    public sealed class HostingEnvironmentController : DiagnosticControllerBase<HostingEnvironment>
    {
        public HostingEnvironmentController(IServiceProvider services)
            : base(services)
        {
        }

        private async Task<DiagnosticStampData> GetHostingEnvironmentPostBody(string hostingEnvironmentName)
        {
            var dataProviders = new DataProviders.DataProviders((DataProviderContext)HttpContext.Items[HostConstants.DataProviderContextKey]);
            dynamic postBody = await dataProviders.Observer.GetHostingEnvironmentPostBody(hostingEnvironmentName);
            JObject bodyObject = (JObject)postBody;
            return bodyObject.ToObject<DiagnosticStampData>();
        }

        [HttpPost(UriElements.Query)]
        public async Task<IActionResult> ExecuteQuery(string subscriptionId, string resourceGroupName, string hostingEnvironmentName, string[] hostNames, string stampName, [FromBody]CompilationPostBody<DiagnosticStampData> jsonBody, string startTime = null, string endTime = null, string timeGrain = null, [FromQuery][ModelBinder(typeof(FormModelBinder))] Form Form = null)
        {
            if (jsonBody == null)
            {
                return BadRequest("Missing body");
            }

            if (jsonBody.Resource == null)
            {
                jsonBody.Resource = await GetHostingEnvironmentPostBody(hostingEnvironmentName);
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            HostingEnvironment ase = await GetHostingEnvironment(subscriptionId, resourceGroupName, hostingEnvironmentName, jsonBody.Resource, startTimeUtc, endTimeUtc);
            return await base.ExecuteQuery(ase, jsonBody, startTime, endTime, timeGrain, Form: Form);
        }

        [HttpPost(UriElements.Detectors)]
        public async Task<IActionResult> ListDetectors(string subscriptionId, string resourceGroupName, string hostingEnvironmentName, [FromBody] DiagnosticStampData postBody)
        {
            if (postBody == null)
            {
                postBody = await GetHostingEnvironmentPostBody(hostingEnvironmentName);
            }

            DateTimeHelper.PrepareStartEndTimeWithTimeGrain(string.Empty, string.Empty, string.Empty, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage);
            HostingEnvironment ase = await GetHostingEnvironment(subscriptionId, resourceGroupName, hostingEnvironmentName, postBody, startTimeUtc, endTimeUtc);
            return await base.ListDetectors(ase);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource)]
        public async Task<IActionResult> GetDetector(string subscriptionId, string resourceGroupName, string hostingEnvironmentName, string detectorId, [FromBody] DiagnosticStampData postBody, string startTime = null, string endTime = null, string timeGrain = null, [FromQuery][ModelBinder(typeof(FormModelBinder))] Form form = null)
        {
            if (postBody == null)
            {
                postBody = await GetHostingEnvironmentPostBody(hostingEnvironmentName);
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            HostingEnvironment ase = await GetHostingEnvironment(subscriptionId, resourceGroupName, hostingEnvironmentName, postBody, startTimeUtc, endTimeUtc);
            return await base.GetDetector(ase, detectorId, startTime, endTime, timeGrain, form: form);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.StatisticsQuery)]
        public async Task<IActionResult> ExecuteSystemQuery(string subscriptionId, string resourceGroupName, string hostingEnvironmentName, string[] hostNames, string stampName, [FromBody]CompilationPostBody<DiagnosticStampData> jsonBody, string detectorId, string dataSource = null, string timeRange = null)
        {
            HostingEnvironment ase = new HostingEnvironment(subscriptionId, resourceGroupName, hostingEnvironmentName);
            return await base.ExecuteQuery(ase, jsonBody, null, null, null, detectorId, dataSource, timeRange);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.Statistics + UriElements.StatisticsResource)]
        public async Task<IActionResult> GetSystemInvoker(string subscriptionId, string resourceGroupName, string hostingEnvironmentName, string detectorId, string invokerId, string dataSource = null, string timeRange = null)
        {
            return await base.GetSystemInvoker(GetResource(subscriptionId, resourceGroupName, hostingEnvironmentName), detectorId, invokerId, dataSource, timeRange);
        }

        [HttpPost(UriElements.Insights)]
        public async Task<IActionResult> GetInsights(string subscriptionId, string resourceGroupName, string hostingEnvironmentName, [FromBody] DiagnosticStampData postBody, string pesId, string supportTopicId = null, string startTime = null, string endTime = null, string timeGrain = null)
        {
            if (postBody == null)
            {
                postBody = await GetHostingEnvironmentPostBody(hostingEnvironmentName);
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            HostingEnvironment ase = await GetHostingEnvironment(subscriptionId, resourceGroupName, hostingEnvironmentName, postBody, startTimeUtc, endTimeUtc);
            return await base.GetInsights(ase, pesId, supportTopicId, startTime, endTime, timeGrain);
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
        public async Task<IActionResult> ListGistsAsync(string subscriptionId, string resourceGroupName, string hostingEnvironmentName, [FromBody] DiagnosticStampData postBody)
        {
            if (postBody == null)
            {
                postBody = await GetHostingEnvironmentPostBody(hostingEnvironmentName);
            }

            DateTimeHelper.PrepareStartEndTimeWithTimeGrain(string.Empty, string.Empty, string.Empty, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage);
            HostingEnvironment ase = await GetHostingEnvironment(subscriptionId, resourceGroupName, hostingEnvironmentName, postBody, startTimeUtc, endTimeUtc);
            return await base.ListGists(ase);
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
        public async Task<IActionResult> GetGistAsync(string subscriptionId, string resourceGroupName, string hostingEnvironmentName, string gistId, [FromBody] DiagnosticStampData postBody, string startTime = null, string endTime = null, string timeGrain = null)
        {
            if (postBody == null)
            {
                postBody = await GetHostingEnvironmentPostBody(hostingEnvironmentName);
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            HostingEnvironment ase = await GetHostingEnvironment(subscriptionId, resourceGroupName, hostingEnvironmentName, postBody, startTimeUtc, endTimeUtc);
            return await base.GetGist(ase, gistId, startTime, endTime, timeGrain);
        }
    }
}
