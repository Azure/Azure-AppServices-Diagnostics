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
        /// returns comments from pull request
        /// example
        /// getPullRequestComments darreldonald darreldonald-test-repo darreldonald-test-repo 9
        /// </summary>
        /// <param name="organization"></param>
        /// <param name="project"></param>
        /// <param name="repositoryId"></param>
        /// <param name="pullRequestId"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> getPullRequestCommentsAsync(string organization, string project, string repositoryId, string pullRequestId);
        /// <summary>
        /// returns names of members of the organization and their Ids
        /// example
        /// getOrgMembers darreldonald
        /// </summary>
        /// <param name="organization"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> getOrgMembersAsync(string organization);
        /// <summary>
        /// make a pull request
        /// example
        /// makePullRequest darreldonald darreldonald-test-repo darreldonald-test-repo darreldonald-demo-branch darreldonald-test-branch "demoPR" "demonstrating making a pr" "darreldonald"
        /// </summary>
        /// <param name="organization"></param>
        /// <param name="project"></param>
        /// <param name="repositoryId"></param>
        /// <param name="sourceBranch"></param>
        /// <param name="targetBranch"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="reviewers"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> makePullRequestAsync(string organization, string project, string repositoryId, string sourceBranch, string targetBranch, string title, string description, string reviewers);
        /// <summary>
        /// makes a commit with your changes
        /// example
        /// pushChanges darreldonald darreldonald-test-repo darreldonald-test-repo  darreldonald-demo-branch "C:\Projects\CodingExperiencePOC\DarrelPoc\RepoTestApp2\testfiles\5xxdetector.csx" "/demodetector" "demonstrating how to push code" "add"
        /// </summary>
        /// <param name="organization"></param>
        /// <param name="project"></param>
        /// <param name="repositoryId"></param>
        /// <param name="branch"></param>
        /// <param name="localPath"></param>
        /// <param name="repoPath"></param>
        /// <param name="comment"></param>
        /// <param name="changeType"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> pushChangesAsync(string organization, string project, string repositoryId, string branch, string localPath, string repoPath, string comment, string changeType);
        /// <summary>
        /// pulls recent commits
        /// can be filtered by branch
        /// can set maximum results retreived
        /// example
        /// getCommits darreldonald darreldonald-test-repo darreldonald-test-repo master 5
        /// </summary>
        /// <param name="organization"></param>
        /// <param name="project"></param>
        /// <param name="repositoryId"></param>
        /// <param name="branch"></param>
        /// <param name="maxResults"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> getCommitsAsync(string organization, string project, string repositoryId, string branch, int maxResults = 100);
        /// <summary>
        /// pulls recent commits
        /// can be filtered by branch
        /// can set maximum results retreived
        /// example
        /// getCommits darreldonald darreldonald-test-repo darreldonald-test-repo
        /// </summary>
        /// <param name="organization"></param>
        /// <param name="project"></param>
        /// <param name="repositoryId"></param>
        /// <param name="maxResults"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> getCommitsAsync(string organization, string project, string repositoryId, int maxResults = 100);
        /// <summary>
        /// will retrieve and display the given file from the repository
        /// example
        /// getDetectorCode darreldonald darreldonald-test-repo darreldonald-test-repo "/5xxdetector/5xxdetector.csx"
        /// </summary>
        /// <param name="organization"></param>
        /// <param name="project"></param>
        /// <param name="repositoryId"></param>
        /// <param name="detectorPath"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> getDetectorCodeAsync(string organization, string project, string repositoryId, string detectorPath);
        /// <summary>
        /// displays active pull requests
        /// example
        /// getPullRequests darreldonald darreldonald-test-repo darreldonald-test-repo
        /// </summary>
        /// <param name="organization"></param>
        /// <param name="project"></param>
        /// <param name="repositoryId"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> getPullRequestsAsync(string organization, string project, string repositoryId);
        /// <summary>
        /// displays details of a specific pull request
        /// example
        /// getPullRequest darreldonald darreldonald-test-repo darreldonald-test-repo 9
        /// </summary>
        /// <param name="organization"></param>
        /// <param name="project"></param>
        /// <param name="repositoryId"></param>
        /// <param name="pullRequestId"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> getPullRequestAsync(string organization, string project, string repositoryId, string pullRequestId);
        /// <summary>
        /// returns repositories in the organization
        /// example
        /// getRepositories darreldonald
        /// </summary>
        /// <param name="organization"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> getRepositoriesAsync(string organization);
    }
}
