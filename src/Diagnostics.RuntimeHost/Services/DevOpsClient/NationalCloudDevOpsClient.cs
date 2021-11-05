using Diagnostics.ModelsAndUtils.Models.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.DevOpsClient
{
    public class NationalCloudDevOpsClient : IRepoClient
    {
        public Task<object> GetBranchesAsync(string resourceUri, string requestId)
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

        public Task<List<DevopsFileChange>> GetFilesInCommit(string commitId)
        {
            return null;
        }

        public Task<object> MakePullRequestAsync(string sourceBranch, string targetBranch, string title, string resourceUri, string requestId)
        {
            return null;
        }

        public Task<object> PushChangesAsync(string branch, List<string> files, List<string> repoPaths, string comment, string changeType, string resourceUri, string requestId)
        {
            return null;
        }
    }
}
