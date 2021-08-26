using Diagnostics.RuntimeHost.Services.DevOpsClient;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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

        [HttpGet(UriElements.DevOpsMakePR)]
        public async Task<IActionResult> MakePullRequestAsync(string sourceBranch, string targetBranch, string title, string resourceURI)
        {
            DevOpsResponse response = await _devOpsClient.MakePullRequestAsync(sourceBranch, targetBranch, title);
            return StatusCode((int)response.responseCode, response.result);
        }

        [HttpGet(UriElements.DevOpsPush)]
        public async Task<IActionResult> PushChangesAsync(string branch, string file, string repoPath, string comment, string changeType, string resourceURI)
        {
            DevOpsResponse response = await _devOpsClient.PushChangesAsync(branch, file, repoPath, comment, changeType);
            return StatusCode((int)response.responseCode, response.result);
        }

        [HttpGet(UriElements.DevOpsGetCode)]
        public async Task<IActionResult> GetDetectorCodeAsync(string detectorPath, string resourceURI)
        {
            DevOpsResponse response = await _devOpsClient.GetDetectorCodeAsync(detectorPath);
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
