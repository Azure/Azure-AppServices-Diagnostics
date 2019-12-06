using System;
using System.Threading.Tasks;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Diagnostics.RuntimeHost.Controllers
{
    /// <summary>
    /// Arm resource controller.
    /// </summary>
    [Produces("application/json")]
    [Route(UriElements.ArmResource)]
    public class ArmResourceController : DiagnosticControllerBase<ArmResource>
    {
        public ArmResourceController(IServiceProvider services, IRuntimeContext<ArmResource> runtimeContext)
            : base(services, runtimeContext)
        {
        }

        [HttpPost(UriElements.Query)]
        public Task<IActionResult> ExecuteQuery(string subscriptionId, string resourceGroupName, string provider, string resourceTypeName, string resourceName, [FromBody]CompilationPostBody<dynamic> jsonBody, string startTime = null, string endTime = null, string timeGrain = null, [FromQuery][ModelBinder(typeof(FormModelBinder))] Form Form = null)
        {
            return base.ExecuteQuery(new ArmResource(subscriptionId, resourceGroupName, provider, resourceTypeName, resourceName, GetLocation()), jsonBody, startTime, endTime, timeGrain, Form: Form);
        }

        [HttpPost(UriElements.Detectors)]
        public Task<IActionResult> ListDetectors(string subscriptionId, string resourceGroupName, string provider, string resourceTypeName, string resourceName, [FromBody] dynamic postBody)
        {
            return base.ListDetectors(new ArmResource(subscriptionId, resourceGroupName, provider, resourceTypeName, resourceName, GetLocation()));
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource)]
        public Task<IActionResult> GetDetector(string subscriptionId, string resourceGroupName, string provider, string resourceTypeName, string resourceName, string detectorId, [FromBody] dynamic postBody, string startTime = null, string endTime = null, string timeGrain = null, [FromQuery][ModelBinder(typeof(FormModelBinder))] Form form = null)
        {
            return base.GetDetector(new ArmResource(subscriptionId, resourceGroupName, provider, resourceTypeName, resourceName, GetLocation()), detectorId, startTime, endTime, timeGrain, form: form);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.StatisticsQuery)]
        public Task<IActionResult> ExecuteSystemQuery(string subscriptionId, string resourceGroupName, string provider, string resourceTypeName, string resourceName, [FromBody]CompilationPostBody<dynamic> jsonBody, string detectorId, string dataSource = null, string timeRange = null)
        {
            return base.ExecuteQuery(new ArmResource(subscriptionId, resourceGroupName, provider, resourceTypeName, resourceName, GetLocation()), jsonBody, null, null, null, detectorId, dataSource, timeRange);
        }

        [HttpPost(UriElements.Detectors + UriElements.DetectorResource + UriElements.Statistics + UriElements.StatisticsResource)]
        public Task<IActionResult> GetSystemInvoker(string subscriptionId, string resourceGroupName, string provider, string resourceTypeName, string resourceName, string detectorId, string invokerId, string dataSource = null, string timeRange = null)
        {
            return base.GetSystemInvoker(new ArmResource(subscriptionId, resourceGroupName, provider, resourceTypeName, resourceName, GetLocation()), detectorId, invokerId, dataSource, timeRange);
        }

        [HttpPost(UriElements.Insights)]
        public Task<IActionResult> GetInsights(string subscriptionId, string resourceGroupName, string provider, string resourceTypeName, string resourceName, [FromBody] dynamic postBody, string pesId, string supportTopicId = null, string startTime = null, string endTime = null, string timeGrain = null)
        {
            return base.GetInsights(new ArmResource(subscriptionId, resourceGroupName, provider, resourceTypeName, resourceName, GetLocation()), pesId, supportTopicId, startTime, endTime, timeGrain);
        }

        /// <summary>
        /// Publish package.
        /// </summary>
        /// <param name="pkg">The package.</param>
        /// <returns>Task for publishing package.</returns>
        [HttpPost(UriElements.Publish)]
        public Task<IActionResult> PublishPackageAsync([FromBody] Package pkg)
        {
            return PublishPackage(pkg);
        }

        /// <summary>
        /// List all gists.
        /// </summary>
        /// <returns>Task for listing all gists.</returns>
        [HttpPost(UriElements.Gists)]
        public Task<IActionResult> ListGistsAsync(string subscriptionId, string resourceGroupName, string provider, string resourceTypeName, string resourceName, [FromBody] dynamic postBody)
        {
            return base.ListGists(new ArmResource(subscriptionId, resourceGroupName, provider, resourceTypeName, resourceName, GetLocation()));
        }

        /// <summary>
        /// List the gist.
        /// </summary>
        /// <param name="subscriptionId">Subscription id.</param>
        /// <param name="resourceGroupName">Resource group name.</param>
        /// <param name="provider">Provider name.</param>
        /// <param name="resourceTypeName">Resource type name.</param>
        /// <param name="resourceName">Resource name.</param>
        /// <param name="gistId">Gist id.</param>
        /// <returns>Task for listing the gist.</returns>
        [HttpPost(UriElements.Gists + UriElements.GistResource)]
        public Task<IActionResult> GetGistAsync(string subscriptionId, string resourceGroupName, string provider, string resourceTypeName, string resourceName, string gistId, [FromBody] dynamic postBody, string startTime = null, string endTime = null, string timeGrain = null)
        {
            return base.GetGist(new ArmResource(subscriptionId, resourceGroupName, provider, resourceTypeName, resourceName, GetLocation()), gistId, startTime, endTime, timeGrain);
        }

        private string GetLocation()
        {
            if (this.Request.Headers.TryGetValue(HeaderConstants.LocationHeader, out var locations) && locations.Count > 0)
            {
                return locations[0];
            }

            return null;
        }
    }
}
