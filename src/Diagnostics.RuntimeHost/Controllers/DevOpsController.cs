using Diagnostics.RuntimeHost.Services.DevOpsClient;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using static Diagnostics.Logger.HeaderConstants;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Authorize]
    [Produces("application/json")]
    [Route(UriElements.DevOps)]
    public class DevOpsController : Controller
    {
        IRepoClient _devOpsClient;

        public DevOpsController(IRepoClient devOpsClient)
        {
            _devOpsClient = devOpsClient;
        }

        [HttpPost(UriElements.DevOpsMakePR)]
        public async Task<IActionResult> MakePullRequestAsync([FromBody]JToken jsonBody)
        {
            string[] fieldNames =
            {
                "sourceBranch",
                "targetBranch",
                "title",
                "resourceUri"
            };

            if (!RequestBodyValidator.ValidateRequestBody(jsonBody, fieldNames, out string validationMessage))
                return BadRequest(validationMessage);

            string sourceBranch = jsonBody[$"sourceBranch"].ToString();
            string targetBranch = jsonBody[$"targetBranch"].ToString();
            string title = jsonBody[$"title"].ToString();
            string resourceUri = jsonBody[$"resourceUri"].ToString();

            object response = await _devOpsClient.MakePullRequestAsync(sourceBranch, targetBranch, title, resourceUri, this.HttpContext.Request.Headers[RequestIdHeaderName]);
            return Ok(response);
        }

        [HttpPost(UriElements.DevOpsPush)]
        public async Task<IActionResult> PushChangesAsync([FromBody]JToken jsonBody)
        {
            string[] fieldNames =
            {
                "branch",
                "files",
                "repoPaths",
                "comment",
                "changeType",
                "resourceUri"
            };

            if (!RequestBodyValidator.ValidateRequestBody(jsonBody, fieldNames, out string validationMessage))
                return BadRequest(validationMessage);

            string branch = jsonBody[$"branch"].ToString();
            List<string> files = jsonBody[$"files"].Select(file => file.ToString()).ToList();
            List<string> repoPaths = jsonBody[$"repoPaths"].Select(path => path.ToString()).ToList();
            string comment = jsonBody[$"comment"].ToString();
            string changeType = jsonBody[$"changeType"].ToString();
            string resourceUri = jsonBody[$"resourceUri"].ToString();

            object response = await _devOpsClient.PushChangesAsync(branch, files, repoPaths, comment, changeType, resourceUri, this.HttpContext.Request.Headers[RequestIdHeaderName]);
            return Ok(response);
        }

        [HttpGet(UriElements.DevOpsGetCode)]
        public async Task<IActionResult> GetFileContentAsync(string filePathInRepo, string resourceUri, string branch)
        {
            object response = await _devOpsClient.GetFileContentAsync(filePathInRepo, resourceUri, this.HttpContext.Request.Headers[RequestIdHeaderName], branch);
            return Ok(response);
        }

        [HttpGet(UriElements.DevOpsGetBranches)]
        public async Task<IActionResult> GetBranchesAsync(string resourceUri)
        {
            object response = await _devOpsClient.GetBranchesAsync(resourceUri, this.HttpContext.Request.Headers[RequestIdHeaderName]);
            return Ok(response);
        }

        [HttpPost(UriElements.DevOpsMerge)]
        public async Task<IActionResult> MergeAsync([FromBody] JToken jsonBody)
        {
            string[] fieldNames =
            {
                "branch",
                "detectorName",
                "resourceUri"
            };

            if (!RequestBodyValidator.ValidateRequestBody(jsonBody, fieldNames, out string validationMessage))
                return BadRequest(validationMessage);

            string branch = jsonBody[$"branch"].ToString();
            string detectorName = jsonBody[$"detectorName"].ToString();
            string resourceUri = jsonBody[$"resourceUri"].ToString();

            object response = await _devOpsClient.MergeAsync(branch, detectorName, resourceUri, this.HttpContext.Request.Headers[RequestIdHeaderName]);
            return Ok(response);
        }
    }
}
