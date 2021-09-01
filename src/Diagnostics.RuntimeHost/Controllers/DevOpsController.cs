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

            object response = await _devOpsClient.MakePullRequestAsync(sourceBranch, targetBranch, title, resourceUri);
            return Ok(response);
        }

        [HttpPut(UriElements.DevOpsPush)]
        public async Task<IActionResult> PushChangesAsync([FromBody]JToken jsonBody)
        {
            string[] fieldNames =
            {
                "branch",
                "file",
                "repoPath",
                "comment",
                "changeType",
                "resourceUri"
            };

            if (!RequestBodyValidator.ValidateRequestBody(jsonBody, fieldNames, out string validationMessage))
                return BadRequest(validationMessage);

            string branch = jsonBody[$"branch"].ToString();
            string file = jsonBody[$"file"].ToString();
            string repoPath = jsonBody[$"repoPath"].ToString();
            string comment = jsonBody[$"comment"].ToString();
            string changeType = jsonBody[$"changeType"].ToString();
            string resourceUri = jsonBody[$"resourceUri"].ToString();

            object response = await _devOpsClient.PushChangesAsync(branch, file, repoPath, comment, changeType, resourceUri);
            return Ok(response);
        }

        [HttpGet(UriElements.DevOpsGetCode)]
        public async Task<IActionResult> GetFileContentAsync(string filePathInRepo, string branch, string resourceUri)
        {
            object response = await _devOpsClient.GetFileContentAsync(filePathInRepo, branch, resourceUri);
            return Ok(response);
        }

        [HttpGet(UriElements.DevOpsGetBranches)]
        public async Task<IActionResult> GetBranchesAsync(string resourceUri)
        {
            object response = await _devOpsClient.GetBranchesAsync(resourceUri);
            return Ok(response);
        }
    }
}
