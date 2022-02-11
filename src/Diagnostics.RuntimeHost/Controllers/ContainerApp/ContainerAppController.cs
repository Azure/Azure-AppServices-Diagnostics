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
using Diagnostics.DataProviders;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Authorize]
    [Produces("application/json")]
    [Route(UriElements.ContainerAppResource)]
    public class ContainerAppController : DiagnosticControllerBase<ContainerApp>
    {
        public ContainerAppController(IServiceProvider services, IRuntimeContext<ContainerApp> runtimeContext, IConfiguration config)
            : base(services, runtimeContext, config)
        {
        }

        [HttpPost(UriElements.Query)]
        [HttpPost("~/" + UriElements.MicrosoftAppContainerAppResource + "/" + UriElements.Query)]
        public async Task<IActionResult> ExecuteQuery(string subscriptionId, string resourceGroupName, string siteName, [FromBody] CompilationPostBody<DiagnosticContainerAppData> jsonBody, string startTime = null, string endTime = null, string timeGrain = null, [FromQuery][ModelBinder(typeof(FormModelBinder))] Form Form = null)
        {
            if (jsonBody == null)
            {
                return BadRequest("Missing body");
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }
            ContainerApp app = await GetContainerAppResource(subscriptionId, resourceGroupName, siteName, jsonBody.Resource);
            return await base.ExecuteQuery(app, jsonBody, startTime, endTime, timeGrain, Form: Form);
        }

        [HttpPost(UriElements.Detectors)]
        [HttpPost("~/" + UriElements.MicrosoftAppContainerAppResource + "/" + UriElements.Detectors)]
        public async Task<IActionResult> ListDetectors(string subscriptionId, string resourceGroupName, string siteName, [FromBody] DiagnosticContainerAppData postBody, [FromQuery(Name = "text")] string text = null, [FromQuery] string l = "")
        {
            var providerName = GetResourceProviderName(subscriptionId, resourceGroupName);
            return await base.ListDetectors(new ContainerApp(subscriptionId, resourceGroupName, providerName, siteName), text, language: l.ToLower());
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource)]
        [HttpPost("~/" + UriElements.MicrosoftAppContainerAppResource + "/" + UriElements.Detectors + UriElements.DetectorResource)]
        public async Task<IActionResult> GetDetector(string subscriptionId, string resourceGroupName, string siteName, string detectorId, [FromBody] DiagnosticContainerAppData postBody, string startTime = null, string endTime = null, string timeGrain = null, [FromQuery][ModelBinder(typeof(FormModelBinder))] Form form = null, [FromQuery] string l = "")
        {
            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }
            ContainerApp app = await GetContainerAppResource(subscriptionId, resourceGroupName, siteName, postBody);
            return await base.GetDetector(app, detectorId, startTime, endTime, timeGrain, form: form, language: l.ToLower());
        }

        [HttpPost(UriElements.DiagnosticReport)]
        [HttpPost("~/" + UriElements.MicrosoftAppContainerAppResource + "/" + UriElements.DiagnosticReport)]
        public async Task<IActionResult> DiagnosticReport(string subscriptionId, string resourceGroupName, string siteName, [FromBody] DiagnosticReportQuery queryBody, string startTime = null, string endTime = null, string timeGrain = null, [FromQuery][ModelBinder(typeof(FormModelBinder))] Form form = null)
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
            ContainerApp app = await GetContainerAppResource(subscriptionId, resourceGroupName, siteName);
            return await base.GetDiagnosticReport(app, queryBody, startTimeUtc, endTimeUtc, timeGrainTimeSpan, form: form);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.StatisticsQuery)]
        [HttpPost("~/" + UriElements.MicrosoftAppContainerAppResource + "/" + UriElements.Detectors + UriElements.DetectorResource + UriElements.StatisticsQuery)]
        public async Task<IActionResult> ExecuteSystemQuery(string subscriptionId, string resourceGroupName, string siteName, [FromBody] CompilationPostBody<DiagnosticContainerAppData> jsonBody, string detectorId, string dataSource = null, string timeRange = null)
        {
            var providerName = GetResourceProviderName(subscriptionId, resourceGroupName);
            return await base.ExecuteQuery(new ContainerApp(subscriptionId, resourceGroupName, providerName, siteName), jsonBody, null, null, null, detectorId, dataSource, timeRange);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.Statistics + UriElements.StatisticsResource)]
        [HttpPost("~/" + UriElements.MicrosoftAppContainerAppResource + "/" + UriElements.Detectors + UriElements.DetectorResource + UriElements.StatisticsQuery + UriElements.StatisticsResource)]
        public async Task<IActionResult> GetSystemInvoker(string subscriptionId, string resourceGroupName, string siteName, string detectorId, string invokerId, string dataSource = null, string timeRange = null)
        {
            var providerName = GetResourceProviderName(subscriptionId, resourceGroupName);
            return await base.GetSystemInvoker(new ContainerApp(subscriptionId, resourceGroupName, providerName, siteName), detectorId, invokerId, dataSource, timeRange);
        }

        [HttpPost(UriElements.Insights)]
        [HttpPost("~/" + UriElements.MicrosoftAppContainerAppResource + "/" + UriElements.Insights)]
        public async Task<IActionResult> GetInsights(string subscriptionId, string resourceGroupName, string siteName, [FromBody] dynamic postBody, string pesId, string supportTopicId = null, string supportTopic = null, string startTime = null, string endTime = null, string timeGrain = null)
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
            var providerName = GetResourceProviderName(subscriptionId, resourceGroupName);
            return await base.GetInsights(new ContainerApp(subscriptionId, resourceGroupName, providerName, siteName), pesId, supportTopicId, startTime, endTime, timeGrain, supportTopic, postBodyString);
        }

        /// <summary>
        /// Publish package.
        /// </summary>
        /// <param name="pkg">The package.</param>
        /// <returns>Task for publishing package.</returns>
        [HttpPost(UriElements.Publish)]
        [HttpPost("~/" + UriElements.MicrosoftAppContainerAppResource + "/" + UriElements.Publish)]
        public async Task<IActionResult> PublishPackageAsync([FromBody] Package pkg)
        {
            return await PublishPackage(pkg);
        }

        /// <summary>
        /// List all gists.
        /// </summary>
        /// <returns>Task for listing all gists.</returns>
        [HttpPost(UriElements.Gists)]
        [HttpPost("~/" + UriElements.MicrosoftAppContainerAppResource + "/" + UriElements.Gists)]
        public async Task<IActionResult> ListGistsAsync(string subscriptionId, string resourceGroupName, string siteName, [FromBody] DiagnosticContainerAppData postBody)
        {
            var providerName = GetResourceProviderName(subscriptionId, resourceGroupName);
            return await base.ListGists(new ContainerApp(subscriptionId, resourceGroupName, providerName, siteName));
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
        [HttpPost("~/" + UriElements.MicrosoftAppContainerAppResource + "/" + UriElements.Gists + UriElements.GistResource)]
        public async Task<IActionResult> GetGistAsync(string subscriptionId, string resourceGroupName, string siteName, string gistId, [FromBody] DiagnosticContainerAppData postBody, string startTime = null, string endTime = null, string timeGrain = null)
        {
            var providerName = GetResourceProviderName(subscriptionId, resourceGroupName);
            return await base.GetGist(new ContainerApp(subscriptionId, resourceGroupName, providerName, siteName), gistId, startTime, endTime, timeGrain);
        }

        private static bool IsPostBodyMissing(DiagnosticContainerAppData postBody)
        {
            return postBody == null || string.IsNullOrWhiteSpace(postBody.ContainerAppName);
        }

        private async Task<DiagnosticContainerAppData> GetContainerAppPostBody(string subscriptionId, string resourceGroupName, string ContainerAppName)
        {
            try
            {
                var dataProviders = new DataProviders.DataProviders((DataProviderContext)HttpContext.Items[HostConstants.DataProviderContextKey]);
                dynamic postBody = await dataProviders.Observer.GetContainerAppPostBody(ContainerAppName);
                List<DiagnosticContainerAppData> objectList = JsonConvert.DeserializeObject<List<DiagnosticContainerAppData>>(JsonConvert.SerializeObject(postBody));
                var appBody = objectList.Find(obj => (obj.SubscriptionName.ToLower() == subscriptionId.ToLower()) && (obj.ResourceGroupName.ToLower() == resourceGroupName.ToLower()));
                return appBody;
            }
            catch
            {
                return null;
            }
        }

        protected async Task<ContainerApp> GetContainerAppResource(string subscriptionId, string resourceGroup, string resourceName, DiagnosticContainerAppData postBody=null)
        {
            if (IsPostBodyMissing(postBody))
            {
                postBody = await GetContainerAppPostBody(subscriptionId, resourceGroup, resourceName);
            }
            var providerName = (postBody != null && postBody.IsInAppNamespace) ? ResourceProviders.App : ResourceProviders.Web;
            return new ContainerApp(subscriptionId, resourceGroup, providerName, resourceName, postBody!=null? postBody.KubeEnvironmentName: null, postBody!=null?postBody.GeoMasterName:null, postBody != null ? postBody.Fqdn : null, postBody != null ? postBody.Location : null);
        }

        private string GetResourceProviderName(string subscriptionId, string resourceGroup)
        {
            var path = HttpContext.Request.Path.Value;
            var prefix = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/";

            var providerNameLastIndex = path.IndexOf('/', prefix.Length);
            var providerName = path.Substring(prefix.Length, providerNameLastIndex - prefix.Length);

            return providerName;
        }
    }
}
