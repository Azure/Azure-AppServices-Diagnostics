using Diagnostics.ModelsAndUtils.Models.Storage;
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
        public async Task<IActionResult> MakePullRequestAsync([FromBody] DevOpsPullRequest pullRequest)
        {
            if (pullRequest == null)
            {
                return BadRequest();
            }

            object response = await _devOpsClient.MakePullRequestAsync(
                pullRequest.SourceBranch,
                pullRequest.TargetBranch,
                pullRequest.Title,
                pullRequest.ResourceUri,
                this.HttpContext.Request.Headers[RequestIdHeaderName]).ConfigureAwait(false);

            return Ok(response);
        }

        [HttpPost(UriElements.DevOpsPush)]
        public async Task<IActionResult> PushChangesAsync([FromBody] DevOpsPushChangeRequest pushChangeRequest)
        {
            List<string> files = pushChangeRequest.Files.ToList();
            List<string> repoPaths = pushChangeRequest.RepoPaths.ToList();

            object response = await _devOpsClient.PushChangesAsync(
                pushChangeRequest.Branch,
                files,
                repoPaths,
                pushChangeRequest.Comment,
                pushChangeRequest.ChangeType,
                pushChangeRequest.ResourceUri,
                this.HttpContext.Request.Headers[RequestIdHeaderName]).ConfigureAwait(false);

            if (response.GetType() != typeof(BadRequestObjectResult))
            {
                return Ok(response);
            }
            else
            {
                return (IActionResult)response;
            }
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
    }
}
