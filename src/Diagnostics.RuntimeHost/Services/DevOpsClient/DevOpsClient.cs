using Diagnostics.DataProviders;
using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Diagnostics.RuntimeHost.Services.DevOpsClient
{
    public class DevOpsClient : IRepoClient
    {
        private string _accessToken;
        private string _organization;
        private string _repoID;
        private string _project;
        private VssCredentials credentials;
        private VssConnection connection;
        private static GitHttpClient gitClient;

        public DevOpsClient(IConfiguration config)
        {
            LoadConfigurations(config);
            credentials = (VssCredentials)(FederatedCredential)new VssBasicCredential("pat", _accessToken);
            connection = new VssConnection(new Uri($"https://dev.azure.com/{_organization}"), credentials);
            gitClient = connection.GetClient<GitHttpClient>();
        }

        private void LoadConfigurations(IConfiguration config)
        {
            _accessToken = config[$"DevOps:PersonalAccessToken"];
            _organization = config[$"DevOps:Organization"];
            _project = config[$"DevOps:Project"];
            _repoID = config[$"DevOps:RepoID"];
        }

        private VersionControlChangeType getChangeType(string changeType)
        {
            switch (changeType.ToLower())
            {
                case "add":
                    return VersionControlChangeType.Add;
                case "edit":
                    return VersionControlChangeType.Edit;
                default:
                    throw new InvalidOperationException($"ChangeType: \"{changeType}\" not Supported");
            }
        }

        private async Task<string> getLastObjectIdAsync(string branch)
        {
            GitQueryCommitsCriteria query = new GitQueryCommitsCriteria()
            {
                ItemVersion = new GitVersionDescriptor()
                {
                    Version = branch,
                },
                Top = 1,
            };

            try
            {
                List<GitCommitRef> commits = await gitClient.GetCommitsAsync(_project, _repoID, query);
                return commits[0].CommitId;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<object> GetBranchesAsync(string resourceUri, string requestId)
        {
            object result = null;

            try
            {
                List<GitBranchStats> branches = await gitClient.GetBranchesAsync(_project, _repoID);
                result = branches.Select(x => x.Name).ToList();
            }
            catch(Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogDevOpsApiException(
                    requestId,
                    resourceUri,
                    ex.Message,
                    ex.GetType().ToString(),
                    ex.StackTrace
                    );
                throw;
            }

            return result;
        }
        
        public async Task<object> GetFileContentAsync(string filePathInRepo, string resourceUri, string requestId, string branch = null)
        {
            object result = null;
            GitVersionDescriptor version = null;

            try
            {
               if (!string.IsNullOrWhiteSpace(branch))
                {
                    version = new GitVersionDescriptor()
                    {
                        Version = branch,
                        VersionType = GitVersionType.Branch
                    };
                }

                GitItem item = await gitClient.GetItemAsync(_project, _repoID, path: filePathInRepo, includeContent: true, versionDescriptor: version);
                result = item.Content;
            }
            catch(Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogDevOpsApiException(
                    requestId,
                    resourceUri,
                    ex.Message,
                    ex.GetType().ToString(),
                    ex.StackTrace
                    );
                throw;
            }

            return result;
        }

        public async Task<object> MakePullRequestAsync(string sourceBranch, string targetBranch, string title, string resourceUri, string requestId)
        {
            GitPullRequest pr = new GitPullRequest();
            object result = null;
            string source = $"refs/heads/{sourceBranch}";
            string target = $"refs/heads/{targetBranch}";
            pr.SourceRefName = source;
            pr.TargetRefName = target;
            pr.Title = title;

            try
            {
                result = await gitClient.CreatePullRequestAsync(pr, _project, _repoID);
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogDevOpsApiException(
                    requestId,
                    resourceUri,
                    ex.Message,
                    ex.GetType().ToString(),
                    ex.StackTrace
                    );
                throw;
            }

            return result;
        }

        public async Task<object> PushChangesAsync(string branch, string file, string repoPath, string comment, string changeType, string resourceUri, string requestId)
        {
            string name = $"refs/heads/{branch}";
            object result = null;

            GitRefUpdate newBranch = new GitRefUpdate()
            {
                Name = name,
                OldObjectId = await getLastObjectIdAsync(branch),
            };

            GitCommitRef newCommit = new GitCommitRef()
            {
                Comment = comment,
                Changes = new GitChange[]
                {
                    new GitChange()
                    {
                        ChangeType = getChangeType(changeType),
                        Item = new GitItem() { Path = repoPath },
                        NewContent = new ItemContent()
                        {
                            Content = file,
                            ContentType = ItemContentType.RawText,
                        },
                    }
                },
            };

            GitPush push = new GitPush()
            {
                RefUpdates = new GitRefUpdate[] { newBranch },
                Commits = new GitCommitRef[] { newCommit },
            };
            try
            {
                result = await gitClient.CreatePushAsync(push, _project, _repoID);
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogDevOpsApiException(
                    requestId,
                    resourceUri,
                    ex.Message,
                    ex.GetType().ToString(),
                    ex.StackTrace
                    );
                throw;
            }

            return result;
        }

        /// <summary>
        /// Gets file change in the given commit.
        /// </summary>
        /// <param name="commitId">Commit id to process.</param>
        public async Task<List<DevopsFileChange>> GetFilesInCommit(string commitId)
        {
            if (string.IsNullOrWhiteSpace(commitId))
                throw new ArgumentNullException("commit id cannot be null");
            CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(DataProviderConstants.DefaultTimeoutInSeconds));
            GitRepository repositoryAsync = await gitClient.GetRepositoryAsync(_project, _repoID, (object)null, tokenSource.Token);
            GitCommitChanges changesAsync = await gitClient.GetChangesAsync(commitId, repositoryAsync.Id, null, null, null, tokenSource.Token);
            List<DevopsFileChange> stringList = new List<DevopsFileChange>();
            foreach (GitChange change in changesAsync.Changes)
            {
                var gitversion = new GitVersionDescriptor
                {
                    Version = commitId,
                    VersionType = GitVersionType.Commit,
                    VersionOptions = GitVersionOptions.None
                };
                if (change.Item.Path.EndsWith(".csx") && (change.ChangeType == VersionControlChangeType.Add || change.ChangeType == VersionControlChangeType.Edit))
                {
                    // hack right now, ideally get from config
                    var detectorId = String.Join(";", Regex.Matches(change.Item.Path, @"\/(.+?)\/")
                                        .Cast<Match>()
                                        .Select(m => m.Groups[1].Value));


                    Task<string> detectorScriptTask =  GetFileContentInCommit(repositoryAsync.Id, change.Item.Path, gitversion);
                    Task<string> packageContentTask =  GetFileContentInCommit(repositoryAsync.Id, $"/{detectorId}/package.json", gitversion);
                    Task<string> metadataContentTask =  GetFileContentInCommit(repositoryAsync.Id, $"/{detectorId}/metadata.json", gitversion);            
                    await Task.WhenAll( new Task[] { detectorScriptTask, packageContentTask, metadataContentTask });
                    string detectorScriptContent = await detectorScriptTask;
                    string packageContent = await packageContentTask;
                    string metadataContent = await metadataContentTask;
                    stringList.Add(new DevopsFileChange
                    {
                        CommitId = commitId,
                        Content = detectorScriptContent,
                        Path = change.Item.Path,
                        PackageConfig = packageContent,
                        Metadata = metadataContent
                    });
                }
                else if (change.Item.Path.EndsWith(".csx") && (change.ChangeType == VersionControlChangeType.Delete))
                {
                    var detectorId = String.Join(";", Regex.Matches(change.Item.Path, @"\/(.+?)\/")
                                        .Cast<Match>()
                                        .Select(m => m.Groups[1].Value));

                    GitCommit gitCommitDetails = await gitClient.GetCommitAsync(commitId, repositoryAsync.Id, null, null, tokenSource.Token);
                    // Get the package.json from the parent commit since at this commit, the file doesn't exist.
                    var packageContent = await GetFileContentInCommit(repositoryAsync.Id, $"/{detectorId}/package.json", new GitVersionDescriptor
                    {
                        Version = gitCommitDetails.Parents.FirstOrDefault(),
                        VersionType = GitVersionType.Commit,
                        VersionOptions = GitVersionOptions.None
                    });
                    // Mark this detector as disabled. 
                    stringList.Add(new DevopsFileChange
                    {
                        CommitId = commitId,
                        Content = "",
                        PackageConfig = packageContent,
                        Path = change.Item.Path,
                        Metadata = "",
                        MarkAsDisabled = true
                    });
                }
            }
            return stringList;
        }

        /// <summary>
        /// Gets the file content as string for the given path and repo at a specific commit.
        /// </summary>
        /// <param name="repoId">Repo guid</param>
        /// <param name="ItemPath">Path of the item</param>
        /// <param name="gitVersionDescriptor">Git version descriptior</param>
        private async Task<string> GetFileContentInCommit(Guid repoId, string ItemPath, GitVersionDescriptor gitVersionDescriptor)
        {
            string content = string.Empty;
            var streamResult = await gitClient.GetItemContentAsync(repoId, ItemPath, null, VersionControlRecursionType.None, null,
                       null, null, gitVersionDescriptor);
            using (var reader = new StreamReader(streamResult))
            {
                content = reader.ReadToEnd();
            }
            return content;
        }

        /// <summary>
        /// Gets file changes to deploy between commits based on date range or commit range.
        /// </summary>
        /// <param name="parameters">The given deployment parameters</param>
        public async Task<List<DevopsFileChange>> GetFilesBetweenCommits(DeploymentParameters parameters)
        {
            var result = new List<DevopsFileChange>();

            string gitFromdate;
            string gitToDate;
            CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(DataProviderConstants.DefaultTimeoutInSeconds));
            GitRepository repositoryAsync = await gitClient.GetRepositoryAsync(_project, _repoID, (object)null, tokenSource.Token);
            if (!string.IsNullOrWhiteSpace(parameters.FromCommitId) && !string.IsNullOrWhiteSpace(parameters.ToCommitId))
            {
                GitCommit fromCommitDetails = await gitClient.GetCommitAsync(parameters.FromCommitId, repositoryAsync.Id);
                GitCommit toCommitDetails = await gitClient.GetCommitAsync(parameters.ToCommitId, repositoryAsync.Id);
                gitFromdate = fromCommitDetails.Committer.Date.ToString();
                gitToDate = toCommitDetails.Committer.Date.ToString();
            }
            else
            {
                gitFromdate = parameters.StartDate;
                gitToDate = parameters.EndDate;
            }
            
            var defaultBranch = repositoryAsync.DefaultBranch.Replace("refs/heads/", "");
            GitVersionDescriptor gitVersionDescriptor = new GitVersionDescriptor
            {
                VersionType = GitVersionType.Branch,
                Version = defaultBranch,
                VersionOptions = GitVersionOptions.None
            };
            List<GitCommitRef> commitsToProcess = await gitClient.GetCommitsAsync(repositoryAsync.Id, new GitQueryCommitsCriteria
            {
                FromDate = gitFromdate,
                ToDate = gitToDate,
                ItemVersion = gitVersionDescriptor
            }, null, null, null, tokenSource.Token);
            foreach (var commit in commitsToProcess)
            {
                var filesinCommit = await GetFilesInCommit(commit.CommitId);
                // Process only the latest file update. 
                if (!result.Select(s => s.Path).Contains(filesinCommit.FirstOrDefault().Path))
                {
                    result.AddRange(filesinCommit);
                }

            }
            return result;
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
