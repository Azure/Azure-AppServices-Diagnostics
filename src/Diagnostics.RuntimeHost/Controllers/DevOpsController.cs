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
        IConfiguration _config;
        IDevOpsClient _devOpsClient;

        private static string _accessToken;

        public DevOpsController(IConfiguration configuration, IDevOpsClient devOpsClient)
        {
            _config = configuration;
            _devOpsClient = devOpsClient;
            LoadConfigurations();
            _devOpsClient.setAccessToken(_accessToken);
        }

        private void LoadConfigurations()
        {
            _accessToken = _config[$"DevOps:PersonalAccessToken"];
        }

        [HttpGet(UriElements.DevOpsPRComments)]
        public Task<HttpResponseMessage> getPullRequestCommentsAsync(string organization, string project, string repositoryId, string pullRequestId)
        {
            return _devOpsClient.getPullRequestCommentsAsync(organization, project, repositoryId, pullRequestId);
        }

        [HttpGet(UriElements.DevOpsOrgMembers)]
        public Task<HttpResponseMessage> getOrgMembersAsync(string organization)
        {
            return _devOpsClient.getOrgMembersAsync(organization);
        }

        [HttpGet(UriElements.DevOpsMakePR)]
        public Task<HttpResponseMessage> makePullRequestAsync(string organization, string project, string repositoryId, string sourceBranch, string targetBranch, string title, string description, string reviewers)
        {
            return _devOpsClient.makePullRequestAsync(organization, project, repositoryId, sourceBranch, targetBranch, title, description, reviewers);
        }

        [HttpGet(UriElements.DevOpsPush)]
        public Task<HttpResponseMessage> pushChangesAsync(string organization, string project, string repositoryId, string branch, string localPath, string repoPath, string comment, string changeType)
        {
            return _devOpsClient.pushChangesAsync(organization, project, repositoryId, branch, localPath, repoPath, comment, changeType);
        }

        [HttpGet(UriElements.DevOpsGetCommits)]
        public Task<HttpResponseMessage> getCommitsAsync(string organization, string project, string repositoryId, string branch, int maxResults = 100)
        {
            return _devOpsClient.getCommitsAsync(organization, project, repositoryId, branch, maxResults);
        }

        [HttpGet(UriElements.DevOpsGetCommits)]
        public Task<HttpResponseMessage> getCommitsAsync(string organization, string project, string repositoryId, int maxResults = 100)
        {
            return _devOpsClient.getCommitsAsync(organization, project, repositoryId, maxResults);
        }

        [HttpGet(UriElements.DevOpsGetCode)]
        public async Task<HttpResponseMessage> getDetectorCodeAsync(string organization, string project, string repositoryId, string detectorPath)
        {
            return await _devOpsClient.getDetectorCodeAsync(organization, project, repositoryId, detectorPath);
        }

        [HttpGet(UriElements.DevOpsGetPRs)]
        public Task<HttpResponseMessage> getPullRequestsAsync(string organization, string project, string repositoryId)
        {
            return _devOpsClient.getPullRequestsAsync(organization, project, repositoryId);
        }

        [HttpGet(UriElements.DevOpsGetPR)]
        public Task<HttpResponseMessage> getPullRequestAsync(string organization, string project, string repositoryId, string pullRequestId)
        {
            return _devOpsClient.getPullRequestAsync(organization, project, repositoryId, pullRequestId);
        }

        [HttpGet(UriElements.DevOpsGetRepo)]
        public Task<HttpResponseMessage> getRepositoriesAsync(string organization)
        {
            return _devOpsClient.getRepositoriesAsync(organization);
        }
    }
}
