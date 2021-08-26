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
        public async Task<IActionResult> makePullRequestAsync(string sourceBranch, string targetBranch, string title, Uri resourceURI)
        {
            DevOpsResponse response = await _devOpsClient.makePullRequestAsync(sourceBranch, targetBranch, title);
            return StatusCode((int)response.responseCode, response.result);
        }

        [HttpGet(UriElements.DevOpsPush)]
        public async Task<IActionResult> pushChangesAsync(string branch, string file, string repoPath, string comment, string changeType, Uri resourceURI)
        {
            DevOpsResponse response = await _devOpsClient.pushChangesAsync(branch, file, repoPath, comment, changeType);
            return StatusCode((int)response.responseCode, response.result);
        }

        [HttpGet(UriElements.DevOpsGetCode)]
        public async Task<IActionResult> getDetectorCodeAsync(string detectorPath, Uri resourceURI)
        {
            DevOpsResponse response = await _devOpsClient.getDetectorCodeAsync(detectorPath);
            return StatusCode((int)response.responseCode, response.result);
        }

        [HttpGet(UriElements.DevOpsGetBranches)]
        public async Task<IActionResult> getBranchesAsync(Uri resourceURI)
        {
            DevOpsResponse response = await _devOpsClient.getBranchesAsync();
            return StatusCode((int)response.responseCode, response.result);
        }
    }
}
