using Diagnostics.ModelsAndUtils.Models.Storage;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.DevOpsClient
{
    public class NationalCloudDevOpsClient : IRepoClient
    {
        public Task<List<(string, bool)>> GetBranchesAsync(string resourceUri, string requestId)
        {
            return null;
        }

        public Task<object> GetFileContentAsync(string filePathInRepo, string resourceUri, string requestId, string branch = null)
        {
            return null;
        }

        public Task<List<DevopsFileChange>> GetFilesBetweenCommits(DeploymentParameters parameters)
        {
            return null;
        }

        public Task<List<DevopsFileChange>> GetFilesInCommit(string commitId, string resourceType)
        {
            return null;
        }

        public Task<ResourceProviderRepoConfig> GetRepoConfigsAsync(string resourceProviderType)
        {
            return null;
        }

        public Task<(GitPullRequest, GitRepository)> MakePullRequestAsync(string sourceBranch, string targetBranch, string title, string resourceUri, string requestId)
        {
            return null;
        }

        public Task<object> PushChangesAsync(string branch, List<string> files, List<string> repoPaths, string comment, string changeType, string resourceUri, string requestId)
        {
            return null;
        }
    }
}
