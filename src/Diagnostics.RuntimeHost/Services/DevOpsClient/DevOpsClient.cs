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
        public class DevOpsResponse
    {
        public object result;
        public HttpStatusCode responseCode;
    }

    class DevOpsClient : IRepoClient
    {
        private static string _accessToken;
        private static string _organization;
        private static string _repoID;
        private static string _project;
        private static VssCredentials credentials;
        private static VssConnection connection;
        private GitHttpClient gitClient;

        public DevOpsClient(IConfiguration config)
        {
            LoadConfigurations(config);
            credentials = (VssCredentials)(FederatedCredential)new VssBasicCredential("pat", _accessToken);
            connection = new VssConnection(new Uri($"https://dev.azure.com/{_organization}"), credentials);
            gitClient = connection.GetClient<GitHttpClient>();
        }

        private static HttpClient client = new HttpClient();
        

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
                    throw (new InvalidOperationException($"ChangeType: \"{changeType}\" not Supported"));
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

        public async Task<DevOpsResponse> getBranchesAsync()
        {
            DevOpsResponse response = new DevOpsResponse();
            object result = null;

            try
            {
                List<GitBranchStats> branches = await gitClient.GetBranchesAsync(_project, _repoID);
                result = branches.Select(x => x.Name).ToList();
            }
            catch (Exception ex)
            {
                result = ex.ToString();
            }
            finally
            {
                response.responseCode = gitClient.LastResponseContext.HttpStatusCode;
                response.result = result;
            }

            return response;
        }
        
        public async Task<DevOpsResponse> getDetectorCodeAsync(string detectorPath)
        {
            DevOpsResponse response = new DevOpsResponse();
            object result = null;

            try
            {
                GitItem item = await gitClient.GetItemAsync(_project, _repoID, path: detectorPath, includeContent: true);
                result = item.Content;
            }
            catch (Exception ex)
            {
                result = ex.ToString();
            }
            finally
            {
                response.responseCode = gitClient.LastResponseContext.HttpStatusCode;
                response.result = result;
            }

            return response;
        }

        public async Task<DevOpsResponse> makePullRequestAsync(string sourceBranch, string targetBranch, string title)
        {
            GitPullRequest pr = new GitPullRequest();
            object result = null;
            DevOpsResponse response = new DevOpsResponse();
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
                result = ex.ToString();
            }
            finally
            {
                response.responseCode = gitClient.LastResponseContext.HttpStatusCode;
                response.result = result;
            }

            return response;
        }

        public async Task<DevOpsResponse> pushChangesAsync(string branch, string file, string repoPath, string comment, string changeType)
        {
            string name = $"refs/heads/{branch}";
            object result = null;
            DevOpsResponse response = new DevOpsResponse();

            try
            {
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

                result = await gitClient.CreatePushAsync(push, _project, _repoID);
            }
            catch (Exception ex)
            {
                result = ex.ToString();
            }
            finally
            {
                response.responseCode = gitClient.LastResponseContext.HttpStatusCode;
                response.result = result;
            }

            return response;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
