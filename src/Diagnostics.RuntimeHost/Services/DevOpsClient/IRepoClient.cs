using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.DevOpsClient
{
    public interface IRepoClient
    {
        /// <summary>
        /// make a pull request
        /// </summary>
        /// <param name="sourceBranch"></param>
        /// <param name="targetBranch"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        Task<DevOpsResponse> MakePullRequestAsync(string sourceBranch, string targetBranch, string title);

        /// <summary>
        /// makes a commit with your changes
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="file"></param>
        /// <param name="repoPath"></param>
        /// <param name="comment"></param>
        /// <param name="changeType"></param>
        /// <returns></returns>
        Task<DevOpsResponse> PushChangesAsync(string branch, string file, string repoPath, string comment, string changeType);

        /// <summary>
        /// will retrieve and display the given file from the repository
        /// </summary>
        /// <param name="detectorPath"></param>
        /// <returns></returns>
        Task<DevOpsResponse> GetDetectorCodeAsync(string detectorPath);

        /// <summary>
        /// gets all of the branches in the repo
        /// </summary>
        /// 
        /// <returns></returns>
        Task<DevOpsResponse> GetBranchesAsync();
    }
}
