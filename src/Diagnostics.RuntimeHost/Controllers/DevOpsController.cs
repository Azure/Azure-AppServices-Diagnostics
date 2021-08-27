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
        public async Task<IActionResult> MakePullRequestAsync(string resourceURI, [FromBody]JToken jsonBody)
        {
            string sourceBranch = jsonBody[$"sourceBranch"].ToString();
            string targetBranch = jsonBody[$"targetBranch"].ToString();
            string title = jsonBody[$"title"].ToString();

            if (string.IsNullOrWhiteSpace(sourceBranch))
                return BadRequest("Missing sourceBranch from body");

            if (string.IsNullOrWhiteSpace(targetBranch))
                return BadRequest("Missing targetBranch  from body");

            if (string.IsNullOrWhiteSpace(title))
                return BadRequest("Missing title from body");

            DevOpsResponse response = await _devOpsClient.MakePullRequestAsync(sourceBranch, targetBranch, title);
            return StatusCode((int)response.responseCode, response.result);
        }

        [HttpPut(UriElements.DevOpsPush)]
        public async Task<IActionResult> PushChangesAsync(string resourceURI, [FromBody]JToken jsonBody)
        {
            string branch = jsonBody[$"branch"].ToString();
            string file = jsonBody[$"file"].ToString();
            string repoPath = jsonBody[$"repoPath"].ToString();
            string comment = jsonBody[$"comment"].ToString();
            string changeType = jsonBody[$"changeType"].ToString();

            if (string.IsNullOrWhiteSpace(branch))
                return BadRequest("Missing branch from body");

            if (string.IsNullOrWhiteSpace(file))
                return BadRequest("Missing file  from body");

            if (string.IsNullOrWhiteSpace(repoPath))
                return BadRequest("Missing repoPath from body");

            if (string.IsNullOrWhiteSpace(comment))
                return BadRequest("Missing comment from body");

            if (string.IsNullOrWhiteSpace(changeType))
                return BadRequest("Missing changeType from body");

            DevOpsResponse response = await _devOpsClient.PushChangesAsync(branch, file, repoPath, comment, changeType);
            return StatusCode((int)response.responseCode, response.result);
        }

        [HttpGet(UriElements.DevOpsGetCode)]
        public async Task<IActionResult> GetFileContentAsync(string filePathInRepo, string branch, string resourceURI)
        {
            DevOpsResponse response = await _devOpsClient.GetFileContentAsync(filePathInRepo, branch);
            return StatusCode((int)response.responseCode, response.result);
        }

        [HttpGet(UriElements.DevOpsGetBranches)]
        public async Task<IActionResult> GetBranchesAsync(string resourceURI)
        {
            DevOpsResponse response = await _devOpsClient.GetBranchesAsync();
            return StatusCode((int)response.responseCode, response.result);
        }
    }
}
