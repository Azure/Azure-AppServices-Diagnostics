using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;

namespace Diagnostics.RuntimeHost.Controllers
{
    // Internal API to be used to communicate with internal processes on the diag role e.g. python processes for Search API
    [Authorize]
    [Produces("application/json")]
    [Route("/" + UriElements.Internal)]
    public class InternalAPIController : Controller
    {
        protected ISourceWatcherService _sourceWatcherService;
        protected IInvokerCacheService _invokerCache;
        private InternalAPIHelper _internalApiHelper;
        private ISearchService _searchService;

        public InternalAPIController(IServiceProvider services)
        {
            this._sourceWatcherService = (ISourceWatcherService)services.GetService(typeof(ISourceWatcherService));
            this._invokerCache = (IInvokerCacheService)services.GetService(typeof(IInvokerCacheService));
            this._searchService = (ISearchService)services.GetService(typeof(ISearchService));
            _internalApiHelper = new InternalAPIHelper();
        }

        /// <summary>
        /// List all detectors.
        /// </summary>
        /// <returns>Task for listing detectors.</returns>
        [HttpGet(UriElements.Detectors)]
        public async Task<IActionResult> ListDetectorsForTraining()
        {
            return Ok(await this.ListDetectorsForTrainingInternal());
        }

        /// <summary>
        /// Log Internal API events.
        /// </summary>
        /// <param name="postBody">Request json body.</param>
        /// <returns>Task for listing detectors.</returns>
        [HttpPost(UriElements.Logger)]
        public async Task<IActionResult> LogEvent([FromBody]InternalEventBody postBody)
        {
            var eventType = postBody.EventType;
            var eventContent = postBody.EventContent;
            switch (postBody.EventType)
            {
                case "UnhandledException":
                    var unhandledException = JsonConvert.DeserializeObject<InternalAPIException>(eventContent);
                    DiagnosticsETWProvider.Instance.LogInternalAPIHandledException(unhandledException.RequestId, unhandledException.ExceptionType, unhandledException.ExceptionDetails);
                    break;
                case "HandledException":
                    var handledException = JsonConvert.DeserializeObject<InternalAPIException>(eventContent);
                    DiagnosticsETWProvider.Instance.LogInternalAPIHandledException(handledException.RequestId, handledException.ExceptionType, handledException.ExceptionDetails);
                    break;
                case "TrainingException":
                    var trainingException = JsonConvert.DeserializeObject<InternalAPITrainingException>(eventContent);
                    DiagnosticsETWProvider.Instance.LogInternalAPITrainingException(trainingException.RequestId, trainingException.TrainingId, trainingException.ProductId, trainingException.ExceptionType, trainingException.ExceptionDetails);
                    break;
                case "Insights":
                    var insights = JsonConvert.DeserializeObject<InternalAPIInsights>(eventContent);
                    DiagnosticsETWProvider.Instance.LogInternalAPIInsights(insights.RequestId, insights.Message);
                    break;
                case "APISummary":
                    var apiSummary = JsonConvert.DeserializeObject<InternalAPISummary>(eventContent);
                    DiagnosticsETWProvider.Instance.LogInternalAPISummary(apiSummary.RequestId, apiSummary.OperationName, apiSummary.StatusCode, apiSummary.LatencyInMilliseconds, apiSummary.StartTime, apiSummary.EndTime, apiSummary.Content);
                    break;
                case "TrainingSummary":
                    var trainingSummary = JsonConvert.DeserializeObject<InternalAPITrainingSummary>(eventContent);
                    DiagnosticsETWProvider.Instance.LogInternalAPITrainingSummary(trainingSummary.RequestId, trainingSummary.TrainingId, trainingSummary.ProductId, trainingSummary.LatencyInMilliseconds, trainingSummary.StartTime, trainingSummary.EndTime, trainingSummary.Content);
                    break;
                default:
                    DiagnosticsETWProvider.Instance.LogInternalAPIMessage(eventContent);
                    break;
            }

            return Ok();
        }

        /// <summary>
        /// Uploads model files to github
        /// </summary>
        /// <param name="modelpath">Model path</param>
        [HttpPost(UriElements.PublishModel)]
        public async Task<IActionResult> PublishModel(string trainingId, [FromBody]string modelpath)
        {
            var commits = _internalApiHelper.GetAllFilesInFolder(modelpath);
            var watcher = _sourceWatcherService.Watcher as GitHubWatcher;
            await watcher._githubClient.CreateOrUpdateFiles(commits, $"Model update trainingId {trainingId}");
            return Ok("Model published Successfully");
        }

        [HttpPost(UriElements.RefreshModel)]
        public async Task<IActionResult> RefreshModel(string productId)
        {
            Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues requestIds);
            var requestId = requestIds.FirstOrDefault().Split(new char[] { ',' })[0];
            var parameters = new Dictionary<string, string>();
            parameters.Add("productId", productId);
            await _sourceWatcherService.Watcher.WaitForFirstCompletion();
            return Ok(await _searchService.TriggerModelRefresh(requestId, parameters));
        }

        [HttpPost(UriElements.UpdateResourceConfig)]
        public async Task<IActionResult> UpdateResourceConfig([FromBody]string resourceConfig)
        {
            var commits = new List<CommitContent>()
            {
                new CommitContent("resourceConfig/config.json", Convert.ToBase64String(Encoding.ASCII.GetBytes(resourceConfig)), Octokit.EncodingType.Base64)
            };
            var watcher = _sourceWatcherService.Watcher as GitHubWatcher;
            await watcher._githubClient.CreateOrUpdateFiles(commits, "Resource config updated");
            return Ok();
        }

        /// <summary>
        /// Trains model defined by model information.
        /// </summary>
        /// <param name="productId">Product Id.</param>
        /// <param name="trainingConfig">Training Config.</param>
        [HttpPost(UriElements.TrainModel)]
        public async Task<IActionResult> TrainModel(string productId, [FromBody]TrainingConfigModel trainingConfigModel)
        {
            Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues requestIds);
            var requestId = requestIds.FirstOrDefault().Split(new char[] { ',' })[0];
            var parameters = new Dictionary<string, string>();
            parameters.Add("productId", productId);
            await _sourceWatcherService.Watcher.WaitForFirstCompletion();
            return Ok(await _searchService.TriggerTraining(requestId, trainingConfigModel.TrainingConfig, parameters));
        }

        /// <summary>
        /// Get the list of all detectors
        /// </summary>
        /// <returns>List of all detectors</returns>
        private async Task<List<DetectorMetadata>> ListDetectorsForTrainingInternal()
        {
            await this._sourceWatcherService.Watcher.WaitForFirstCompletion();
            var allDetectors = _invokerCache.GetAll();
            var filteredData = allDetectors.Select(detector => new DetectorMetadata()
            {
                ResourceFilter = _internalApiHelper.GetResourceParams(detector.ResourceFilter),
                Id = detector.EntryPointDefinitionAttribute.Id,
                Name = detector.EntryPointDefinitionAttribute.Name,
                Description = detector.EntryPointDefinitionAttribute.Description,
                Author = detector.EntryPointDefinitionAttribute.Author,
                Metadata = detector.EntityMetadata.Metadata
            }).ToList();
            return filteredData;
        }
    }
}
