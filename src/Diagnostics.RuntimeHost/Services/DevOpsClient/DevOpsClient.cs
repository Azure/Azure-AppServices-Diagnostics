using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
