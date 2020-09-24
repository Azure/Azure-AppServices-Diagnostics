using System;
using System.Threading.Tasks;
using Diagnostics.DataProviders;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using Microsoft.CSharp.RuntimeBinder;
using Diagnostics.Logger;
using Microsoft.Extensions.Primitives;
using System.Linq;

namespace Diagnostics.RuntimeHost.Controllers
{
    /// <summary>
    /// Sites controller.
    /// </summary>
    [Authorize]
    [Produces("application/json")]
    [Route(UriElements.SitesResource)]
    public sealed class SitesController : SiteControllerBase
    {
        public SitesController(IServiceProvider services, IRuntimeContext<App> runtimeContext)
            : base(services, runtimeContext)
        {
        }

        /// <summary>
        /// Site query.
        /// </summary>
        /// <param name="subscriptionId">Subscription id.</param>
        /// <param name="resourceGroupName">Resource group name.</param>
        /// <param name="siteName">Site name.</param>
        /// <param name="jsonBody">Request json body.</param>
        /// <param name="startTime">Start time.</param>
        /// <param name="endTime">End time.</param>
        /// <param name="timeGrain">Time grain.</param>
        /// <returns>Task for handling post request.</returns>
        [HttpPost(UriElements.Query)]
        public async Task<IActionResult> Post(string subscriptionId, string resourceGroupName, string siteName, [FromBody]CompilationPostBody<DiagnosticSiteData> jsonBody, string startTime = null, string endTime = null, string timeGrain = null, [FromQuery][ModelBinder(typeof(FormModelBinder))] Form Form = null)
        {
            if (jsonBody == null)
            {
                return BadRequest("Missing body");
            }

            if (jsonBody.Resource == null)
            {
                jsonBody.Resource = await GetSitePostBody(subscriptionId, resourceGroupName, siteName);
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            App app = await GetAppResource(subscriptionId, resourceGroupName, siteName, jsonBody.Resource, startTimeUtc, endTimeUtc);
            return await base.ExecuteQuery(app, jsonBody, startTime, endTime, timeGrain, Form: Form);
        }

        /// <summary>
        /// List all detectors.
        /// </summary>
        /// <param name="subscriptionId">Subscription id.</param>
        /// <param name="resourceGroupName">Resource group name.</param>
        /// <param name="siteName">Site name.</param>
        /// <param name="postBody">Request json body.</param>
        /// <returns>Task for listing detectors.</returns>
        [HttpPost(UriElements.Detectors)]
        public async Task<IActionResult> ListDetectors(string subscriptionId, string resourceGroupName, string siteName, [FromBody] DiagnosticSiteData postBody, [FromQuery(Name = "text")] string text = null)
        {
            if (IsPostBodyMissing(postBody))
            {
                postBody = await GetSitePostBody(subscriptionId, resourceGroupName, siteName);
            }

            DateTimeHelper.PrepareStartEndTimeWithTimeGrain(string.Empty, string.Empty, string.Empty, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage);
            App app = await GetAppResource(subscriptionId, resourceGroupName, siteName, postBody, startTimeUtc, endTimeUtc);
            return await base.ListDetectors(app, text);
        }

        /// <summary>
        /// Get detector.
        /// </summary>
        /// <param name="subscriptionId">Subscription id.</param>
        /// <param name="resourceGroupName">Resource group name.</param>
        /// <param name="siteName">Site name.</param>
        /// <param name="detectorId">Detector id.</param>
        /// <param name="postBody">Request post body.</param>
        /// <param name="startTime">Start time.</param>
        /// <param name="endTime">End time.</param>
        /// <param name="timeGrain">Time grain.</param>
        /// <returns>Task for getting detector.</returns>
        [HttpPost(UriElements.Detectors + UriElements.DetectorResource)]
        public async Task<IActionResult> GetDetector(string subscriptionId, string resourceGroupName, string siteName, string detectorId, [FromBody] DiagnosticSiteData postBody, string startTime = null, string endTime = null, string timeGrain = null, [FromQuery][ModelBinder(typeof(FormModelBinder))] Form form = null)
        {
            if (IsPostBodyMissing(postBody))
            {
                postBody = await GetSitePostBody(subscriptionId, resourceGroupName, siteName);
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            App app = await GetAppResource(subscriptionId, resourceGroupName, siteName, postBody, startTimeUtc, endTimeUtc);
            return await base.GetDetector(app, detectorId, startTime, endTime, timeGrain, form: form);
        }

        /// <summary>
        /// Execute system query.
        /// </summary>
        /// <param name="subscriptionId">Subscription id.</param>
        /// <param name="resourceGroupName">Resource group name.</param>
        /// <param name="siteName">Site name.</param>
        /// <param name="jsonBody">Request json body.</param>
        /// <param name="detectorId">Detector id.</param>
        /// <param name="dataSource">Data source.</param>
        /// <param name="timeRange">Time range.</param>
        /// <returns>Task for executing system query.</returns>
        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.StatisticsQuery)]
        public async Task<IActionResult> ExecuteSystemQuery(string subscriptionId, string resourceGroupName, string siteName, [FromBody]CompilationPostBody<DiagnosticSiteData> jsonBody, string detectorId, string dataSource = null, string timeRange = null)
        {
            App app = new App(subscriptionId, resourceGroupName, siteName);
            return await base.ExecuteQuery(app, jsonBody, null, null, null, detectorId, dataSource, timeRange);
        }

        /// <summary>
        /// Get system invoker.
        /// </summary>
        /// <param name="subscriptionId">Subscription id.</param>
        /// <param name="resourceGroupName">Resource group name.</param>
        /// <param name="siteName">Site name.</param>
        /// <param name="detectorId">Detector id.</param>
        /// <param name="invokerId">Invoker id.</param>
        /// <param name="dataSource">Data source.</param>
        /// <param name="timeRange">Time range.</param>
        /// <returns>Task for getting system invoker.</returns>
        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.Statistics + UriElements.StatisticsResource)]
        public async Task<IActionResult> GetSystemInvoker(string subscriptionId, string resourceGroupName, string siteName, string detectorId, string invokerId, string dataSource = null, string timeRange = null)
        {
            return await base.GetSystemInvoker(GetResource(subscriptionId, resourceGroupName, siteName), detectorId, invokerId, dataSource, timeRange);
        }

        /// <summary>
        /// Get insights.
        /// </summary>
        /// <param name="subscriptionId">Subscription id.</param>
        /// <param name="resourceGroupName">Resource group name.</param>
        /// <param name="siteName">Site name.</param>
        /// <param name="postBody">Request post body.</param>
        /// <param name="pesId">Pes id.</param>
        /// <param name="supportTopicId">Supported topic id.</param>
        /// <param name="startTime">Start time.</param>
        /// <param name="endTime">End time.</param>
        /// <param name="timeGrain">Time grain.</param>
        /// <returns>Task for getting insights.</returns>
        [HttpPost(UriElements.Insights)]
        public async Task<IActionResult> GetInsights(string subscriptionId, string resourceGroupName, string siteName, [FromBody] dynamic postBody, string pesId = null, string supportTopicId = null, string supportTopic = null, string startTime = null, string endTime = null, string timeGrain = null)
        {
            var diagnosticSitePostBody = JsonConvert.DeserializeObject<DiagnosticSiteData>(JsonConvert.SerializeObject(postBody));
            if (IsPostBodyMissing(diagnosticSitePostBody))
            {
                try
                {
                    diagnosticSitePostBody = await GetSitePostBody(subscriptionId, resourceGroupName, siteName);
                }
                catch (Exception e)
                {
                    string path = Request.Path.Value;

                    // For some requests which are querying on the sites that no longer exist. For these cases, instead of
                    // returnning a 500, we log it to kusto and return a 404.
                    if (e.Message.StartsWith("Could not get admin sites") || e.Message.StartsWith("Admin Sites response did not contain"))
                    {
                        string requestId = null;
                        if (Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues values) && values != default(StringValues) && values.Count > 0)
                        {
                            requestId = values.FirstOrDefault().Split(new char[] { ',' })[0] ?? string.Empty;
                        }
                        // either observer adminsites api returns a 404 or the sites api returns do not match the subscriptionId and/or resourceGroupName
                        Logger.DiagnosticsETWProvider.Instance.LogRuntimeHostHandledException(
                            requestId,
                            $"{nameof(SitesController)}.{nameof(GetInsights)}",
                            subscriptionId,
                            resourceGroupName,
                            siteName,
                            "SiteNotFoundForGetInsightsRequestFromASC",
                            $"Observer adminsites API could not find the site subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/sites/{siteName}/");

                        return NotFound();
                    }
                    else
                    {
                        throw e;
                    }
                }
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage, true))
            {
                return BadRequest(errorMessage);
            }

            App app = await GetAppResource(subscriptionId, resourceGroupName, siteName, diagnosticSitePostBody, startTimeUtc, endTimeUtc);
            string postBodyString;
            try
            {
                postBodyString = JsonConvert.SerializeObject(postBody.Parameters);
            }
            catch (RuntimeBinderException)
            {
                postBodyString = "";
            }
            return await base.GetInsights(app, pesId, supportTopicId, startTime, endTime, timeGrain, supportTopic, postBodyString);
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
        public async Task<IActionResult> ListGistsAsync(string subscriptionId, string resourceGroupName, string siteName, [FromBody] DiagnosticSiteData postBody)
        {
            if (IsPostBodyMissing(postBody))
            {
                postBody = await GetSitePostBody(subscriptionId, resourceGroupName, siteName);
            }

            DateTimeHelper.PrepareStartEndTimeWithTimeGrain(string.Empty, string.Empty, string.Empty, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage);
            App app = await GetAppResource(subscriptionId, resourceGroupName, siteName, postBody, startTimeUtc, endTimeUtc);

            return await ListGists(app);
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
        public async Task<IActionResult> GetGistAsync(string subscriptionId, string resourceGroupName, string siteName, string gistId, [FromBody] DiagnosticSiteData postBody, string startTime = null, string endTime = null, string timeGrain = null)
        {
            if (IsPostBodyMissing(postBody))
            {
                postBody = await GetSitePostBody(subscriptionId, resourceGroupName, siteName);
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            App app = await GetAppResource(subscriptionId, resourceGroupName, siteName, postBody, startTimeUtc, endTimeUtc);
            return await base.GetGist(app, gistId, startTime, endTime, timeGrain);
        }

        /// <summary>
        /// Get site diagnostics properties
        /// </summary>
        /// <param name="subscriptionId">Subscription Id.</param>
        /// <param name="resourceGroupName">Resource Group Name.</param>
        /// <param name="siteName">Site name.</param
        /// <returns>Site Stack.</returns>
        [HttpPost(UriElements.AppStack)]        
        public async Task<IActionResult> GetAppStack(string subscriptionId, string resourceGroupName, string siteName)
        {
            return Ok(await _siteService.GetApplicationStack(subscriptionId, resourceGroupName, siteName, (DataProviderContext)HttpContext.Items[HostConstants.DataProviderContextKey]));  
        }

        private static bool IsPostBodyMissing(DiagnosticSiteData postBody)
        {
            return postBody == null || string.IsNullOrWhiteSpace(postBody.Name);
        }

        private async Task<DiagnosticSiteData> GetSitePostBody(string subscriptionId, string resourceGroupName, string siteName)
        {
            var dataProviders = new DataProviders.DataProviders((DataProviderContext)HttpContext.Items[HostConstants.DataProviderContextKey]);
            string stampName = await dataProviders.Observer.GetStampName(subscriptionId, resourceGroupName, siteName);
            dynamic postBody = await dataProviders.Observer.GetSitePostBody(stampName, siteName);
            JObject bodyObject = (JObject)postBody;
            var sitePostBody = bodyObject.ToObject<DiagnosticSiteData>();
            return sitePostBody;
        }
    }
}
