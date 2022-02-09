using Diagnostics.DataProviders;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using Diagnostics.RuntimeHost.Services.StorageService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private static GitHttpClient defaultClient;
        private ConcurrentDictionary<string, ResourceProviderRepoConfig> resourceProviderMapping = new ConcurrentDictionary<string, ResourceProviderRepoConfig>();
        private ConcurrentDictionary<string, dynamic> dictionary = new ConcurrentDictionary<string, dynamic>();
        private IStorageService storageService;
        private Task configDownloadTask;
        private readonly string defaultPath = "/";

        public DevOpsClient(IConfiguration config, IStorageService storageServiceRef)
        {
            storageService = storageServiceRef;
            LoadConfigurations(config);
            credentials = (VssCredentials)(FederatedCredential)new VssBasicCredential("pat", _accessToken);
            connection = new VssConnection(new Uri($"https://dev.azure.com/{_organization}"), credentials);
            defaultClient = connection.GetClient<GitHttpClient>();
          
        }

        private void LoadConfigurations(IConfiguration config)
        {
            _accessToken = config[$"DevOps:PersonalAccessToken"];
            _organization = config[$"DevOps:Organization"];
            _project = config[$"DevOps:Project"];
            _repoID = config[$"DevOps:RepoID"];
            configDownloadTask = LoadResourceProviderConfigStorage();      
        }

        private async Task LoadResourceProviderConfigStorage()
        {
            byte[] repoConfigBuffer = await storageService.GetResourceProviderConfig();
            string repoConfig = Encoding.UTF8.GetString(repoConfigBuffer, 0, repoConfigBuffer.Length);
            var resourceProviderRepoConfigs = JsonConvert.DeserializeObject<List<ResourceProviderRepoConfig>>(repoConfig);
            foreach (var resourceProviderRepoConfig in resourceProviderRepoConfigs)
            {
                string resourceProvider = resourceProviderRepoConfig.ResourceProvider.ToLower();
                if (!string.IsNullOrWhiteSpace(resourceProvider) && !resourceProviderMapping.ContainsKey(resourceProvider))
                {
                    resourceProviderMapping.TryAdd(resourceProvider.ToLower(), resourceProviderRepoConfig);
                }
            }
        }

        private async Task<ResourceProviderRepoConfig> GetConfigByResourceUri(string armUri)
        {
            string resourceProvider = UriUtilities.GetResourceProviderFromUri(armUri);
            return await GetConfigbyResourceProvider(resourceProvider);
        }

        private async Task<ResourceProviderRepoConfig> GetConfigbyResourceProvider(string resourceProvider)
        {
            // Check the cache if resource provider is present
            if (resourceProviderMapping.ContainsKey(resourceProvider.ToLower()))
            {
                return resourceProviderMapping[resourceProvider.ToLower()];
            }
            else // Attempt to check if storage has the config for this resource provider type.
            {
                byte[] repoConfigBuffer = await storageService.GetResourceProviderConfig();
                string repoConfig = Encoding.UTF8.GetString(repoConfigBuffer, 0, repoConfigBuffer.Length);
                var resourceProviderRepoConfigs = JsonConvert.DeserializeObject<List<ResourceProviderRepoConfig>>(repoConfig);
                foreach (var resourceProviderRepoConfig in resourceProviderRepoConfigs)
                {
                    string currentResourceProvider = resourceProviderRepoConfig.ResourceProvider.ToLower();
                    if (!string.IsNullOrWhiteSpace(currentResourceProvider) && currentResourceProvider.Equals(resourceProvider.ToLower(), StringComparison.CurrentCultureIgnoreCase)
                        && !resourceProviderMapping.ContainsKey(currentResourceProvider.ToLower()))
                    {
                        var creds = (VssCredentials)(FederatedCredential)new VssBasicCredential("pat", _accessToken);
                        var connection = new VssConnection(new Uri($"https://dev.azure.com/{resourceProviderRepoConfig.Organization}"), creds);
                        var gitClient = connection.GetClient<GitHttpClient>();
                        resourceProviderMapping.TryAdd(currentResourceProvider.ToLower(), resourceProviderRepoConfig);
                        return resourceProviderMapping[currentResourceProvider.ToLower()];
                    }
                }
            }
            return new ResourceProviderRepoConfig
            {
                ResourceProvider = resourceProvider,
                AutoMerge = false,
                FolderPath = defaultPath,
                Organization = _organization,
                Project = _project,
                Repository = _repoID
            };
        }

        private VersionControlChangeType getChangeType(string changeType)
        {
            switch (changeType.ToLower())
            {
                case "add":
                    return VersionControlChangeType.Add;
                case "edit":
                    return VersionControlChangeType.Edit;
                case "delete":
                    return VersionControlChangeType.Delete;
                default:
                    throw new InvalidOperationException($"ChangeType: \"{changeType}\" not Supported");
            }
        }

        private async Task<string> getLastObjectIdAsync(string branch, string requestId, string resourceUri)
        {
            await configDownloadTask;
            ResourceProviderRepoConfig resourceProviderRepoConfig = await GetConfigByResourceUri(resourceUri);
            if (!string.IsNullOrWhiteSpace(branch)) dictionary.TryAdd(requestId + "--query", new GitQueryCommitsCriteria()
            {
                ItemVersion = new GitVersionDescriptor()
                {
                    Version = branch,
                },
                Top = 1,
            });
            else 
            {             
                dictionary.TryAdd(requestId + "--repository", await defaultClient.GetRepositoryAsync(resourceProviderRepoConfig.Project, resourceProviderRepoConfig.Repository));
                dictionary[requestId + "--query"] = new GitQueryCommitsCriteria()
                {
                    ItemVersion = new GitVersionDescriptor()
                    {
                        Version = dictionary[requestId + "--repository"].DefaultBranch.Replace("refs/heads/", ""),
                    },
                Top = 1,
                };
            }

            try
            {
                dictionary.TryAdd(requestId + "--commits", await defaultClient.GetCommitsAsync(resourceProviderRepoConfig.Project, resourceProviderRepoConfig.Repository, dictionary[requestId + "--query"]));
                return dictionary[requestId + "--commits"][0].CommitId;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<(string, bool)>> GetBranchesAsync(string resourceUri, string requestId)
        {
            dictionary.TryAdd(requestId + "--result", null);
            await configDownloadTask;
            ResourceProviderRepoConfig resourceProviderRepoConfig = await GetConfigByResourceUri(resourceUri);
            try
            {
                dictionary.TryAdd(requestId + "--branches", await defaultClient.GetBranchesAsync(resourceProviderRepoConfig.Project, resourceProviderRepoConfig.Repository));
                dictionary[requestId + "--result"] = ((List<GitBranchStats>) dictionary[requestId + "--branches"]).Select(x => (x.Name, x.IsBaseVersion)).ToList();
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

            return dictionary[requestId + "--result"];
        }
        
        public async Task<object> GetFileContentAsync(string filePathInRepo, string resourceUri, string requestId, string branch = null)
        {
            dictionary.TryAdd(requestId + "--result", null);
            dictionary.TryAdd(requestId + "--version", null);
            await configDownloadTask;
            ResourceProviderRepoConfig resourceProviderRepoConfig = await GetConfigByResourceUri(resourceUri);
            try
            {
               if (!string.IsNullOrWhiteSpace(branch))
                {
                    dictionary[requestId + "--version"] = new GitVersionDescriptor()
                    {
                        Version = branch,
                        VersionType = GitVersionType.Branch
                    };
                }
                if (!resourceProviderRepoConfig.FolderPath.Equals(defaultPath))
                {
                    filePathInRepo = $"{resourceProviderRepoConfig.FolderPath}/{filePathInRepo}"; 
                }
                dictionary.TryAdd(requestId + "--item", await defaultClient.GetItemAsync(resourceProviderRepoConfig.Project, resourceProviderRepoConfig.Repository, path: filePathInRepo, includeContent: true, versionDescriptor: dictionary[requestId + "--version"]));
                dictionary[requestId + "--result"] = dictionary[requestId + "--item"].Content;
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

            return dictionary[requestId + "--result"];
        }

        public async Task<(GitPullRequest, GitRepository)> MakePullRequestAsync(string sourceBranch, string targetBranch, string title, string resourceUri, string requestId)
        {
            dictionary.TryAdd(requestId + "--pr", new GitPullRequest());
            dictionary.TryAdd(requestId + "--result", null);
            dictionary.TryAdd(requestId + "--source", $"refs/heads/{sourceBranch}");
            dictionary.TryAdd(requestId + "--target", $"refs/heads/{targetBranch}");

            dictionary[requestId + "--pr"].SourceRefName = dictionary[requestId + "--source"];
            dictionary[requestId + "--pr"].TargetRefName = dictionary[requestId + "--target"];
            dictionary[requestId + "--pr"].Title = title;
            await configDownloadTask;
            ResourceProviderRepoConfig resourceProviderRepoConfig = await GetConfigByResourceUri(resourceUri);
            try
            {
                dictionary[requestId + "--prList"] = await GetPRListAsync(dictionary[requestId + "--source"], dictionary[requestId + "--target"], requestId, resourceUri);
                if (dictionary[requestId + "--prList"].Count == 0)
                {
                    dictionary[requestId + "--result"] = await defaultClient.CreatePullRequestAsync(dictionary[requestId + "--pr"], resourceProviderRepoConfig.Project, resourceProviderRepoConfig.Repository);
                }
                else
                {
                    dictionary[requestId + "--result"] = dictionary[requestId + "--prList"][0];
                }

                dictionary.TryAdd(requestId + "--repository", await defaultClient.GetRepositoryAsync(resourceProviderRepoConfig.Project, resourceProviderRepoConfig.Repository));
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

            return (dictionary[requestId + "--result"], dictionary[requestId + "--repository"]);
        }

        private async Task<List<GitPullRequest>> GetPRListAsync(string source, string target, string requestId, string resourceUri)
        {
            dictionary.TryAdd(requestId + "--searchCriteria", new GitPullRequestSearchCriteria()
            {
                TargetRefName = target,
                SourceRefName = source,
                IncludeLinks = true
            });
            await configDownloadTask;

            ResourceProviderRepoConfig resourceProviderRepoConfig = await GetConfigByResourceUri(resourceUri);

            return await defaultClient.GetPullRequestsAsync(resourceProviderRepoConfig.Project, resourceProviderRepoConfig.Repository, dictionary[requestId + "--searchCriteria"]);
        }

        public async Task<object> PushChangesAsync(string branch, List<string> files, List<string> repoPaths, string comment, string changeType, string resourceUri, string requestId)
        {
            await configDownloadTask;
            ResourceProviderRepoConfig resourceProviderRepoConfig = await GetConfigByResourceUri(resourceUri);

            dictionary.TryAdd(requestId + "--name", $"refs/heads/{branch}");
            dictionary.TryAdd(requestId + "--result", null);

            dictionary.TryAdd(requestId + "--newBranch", new GitRefUpdate()
            {
                Name = dictionary[requestId + "--name"],
                OldObjectId = await getLastObjectIdAsync(branch, requestId, resourceUri),
            });
            
            //if getting OldObjectId fails with branch try again from default branch
            if (dictionary[requestId + "--newBranch"].OldObjectId == null)
            {
                dictionary[requestId + "--newBranch"].OldObjectId = await getLastObjectIdAsync(null, requestId, resourceUri);
            }

            dictionary.TryAdd(requestId + "--commitChanges", new GitChange[files.Count]);
            bool isDefaultPath = resourceProviderRepoConfig.FolderPath.Equals(defaultPath);
            if (getChangeType(changeType) == VersionControlChangeType.Delete)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    dictionary[requestId + "--commitChanges"][i] = new GitChange()
                    {
                        ChangeType = getChangeType(changeType),
                        Item = new GitItem() { Path = isDefaultPath ? repoPaths[i] : $"{resourceProviderRepoConfig.FolderPath}/{repoPaths[i]}" }
                    };
                }
            }
            else
            {
                for (int i = 0; i < files.Count; i++)
                {
                    dictionary[requestId + "--commitChanges"][i] = new GitChange()
                    {
                        ChangeType = getChangeType(changeType),
                        Item = new GitItem() { Path = isDefaultPath ? repoPaths[i] : $"{resourceProviderRepoConfig.FolderPath}/{repoPaths[i]}" },
                        NewContent = new ItemContent()
                        {
                            Content = files[i],
                            ContentType = ItemContentType.RawText,
                        },
                    };
                }
            }

            

            dictionary.TryAdd(requestId + "--newCommit", new GitCommitRef()
            {
                Comment = comment,
                Changes = dictionary[requestId + "--commitChanges"]
            });

            dictionary.TryAdd(requestId + "--push", new GitPush()
            {
                RefUpdates = new GitRefUpdate[] { dictionary[requestId+"--newBranch"] },
                Commits = new GitCommitRef[] { dictionary[requestId + "--newCommit"] },
            });

            try
            {
                dictionary[requestId + "--result"] = await defaultClient.CreatePushAsync(dictionary[requestId + "--push"], resourceProviderRepoConfig.Project, resourceProviderRepoConfig.Repository);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("specified in the add operation already exists. Please specify a new path.")){
                    return new BadRequestObjectResult("Detector with this ID already exists. Please use a new ID");
                }
                else
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
            }

            return dictionary[requestId + "--result"];
        }

        /// <summary>
        /// Gets file change in the given commit.
        /// </summary>
        /// <param name="commitId">Commit id to process.</param>
        public async Task<List<DevopsFileChange>> GetFilesInCommit(string commitId, string resourceProvider)
        {
            if (string.IsNullOrWhiteSpace(commitId))
                throw new ArgumentNullException("commit id cannot be null");
            await configDownloadTask;
            ResourceProviderRepoConfig resourceProviderRepoConfig = await GetConfigbyResourceProvider(resourceProvider);

            CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(DataProviderConstants.DefaultTimeoutInSeconds));
            GitRepository repositoryAsync = await defaultClient.GetRepositoryAsync(resourceProviderRepoConfig.Project, resourceProviderRepoConfig.Repository, (object)null, tokenSource.Token);
            GitCommitChanges changesAsync = await defaultClient.GetChangesAsync(commitId, repositoryAsync.Id, null, null, null, tokenSource.Token);
            List<DevopsFileChange> stringList = new List<DevopsFileChange>();
            foreach (GitChange change in changesAsync.Changes)
            {
                var diagEntity = new DiagEntity();
                var gitversion = new GitVersionDescriptor
                {
                    Version = commitId,
                    VersionType = GitVersionType.Commit,
                    VersionOptions = GitVersionOptions.None
                };
                if (change.Item.Path.EndsWith(".csx") && (change.ChangeType == VersionControlChangeType.Add || change.ChangeType == VersionControlChangeType.Edit))
                {
                    string detectorId = Path.GetFileNameWithoutExtension(change.Item.Path);

                    Task<string> detectorScriptTask = GetFileContentInCommit(repositoryAsync.Id, change.Item.Path, gitversion, resourceProvider);
                    Task<string> packageContentTask = GetFileContentInCommit(repositoryAsync.Id, $"{resourceProviderRepoConfig.FolderPath}/{detectorId}/package.json", gitversion, resourceProvider);
                    Task<string> metadataContentTask = GetFileContentInCommit(repositoryAsync.Id, $"{resourceProviderRepoConfig.FolderPath}/{detectorId}/metadata.json", gitversion, resourceProvider);
                    await Task.WhenAll(new Task[] { detectorScriptTask, packageContentTask, metadataContentTask });
                    string detectorScriptContent = await detectorScriptTask;
                    string packageContent = await packageContentTask;
                    string metadataContent = await metadataContentTask;
                    if (packageContent != null)
                    {
                        diagEntity = JsonConvert.DeserializeObject<DiagEntity>(packageContent);
                    } else
                    {
                        throw new Exception("Package.json cannot be empty or null");
                    }
                    stringList.Add(new DevopsFileChange
                    {
                        CommitId = commitId,
                        Content = detectorScriptContent,
                        Path = change.Item.Path,
                        PackageConfig = packageContent,
                        Metadata = metadataContent,
                        Id = diagEntity.DetectorId
                    });
                }
                else if (change.Item.Path.EndsWith(".csx") && (change.ChangeType == VersionControlChangeType.Delete))
                {
                    var detectorId = Path.GetFileNameWithoutExtension(change.Item.Path);

                    GitCommit gitCommitDetails = await defaultClient.GetCommitAsync(commitId, repositoryAsync.Id, null, null, tokenSource.Token);
                    // Get the package.json from the parent commit since at this commit, the file doesn't exist.
                    var packageContent = await GetFileContentInCommit(repositoryAsync.Id, $"{resourceProviderRepoConfig.FolderPath}/{detectorId}/package.json", new GitVersionDescriptor
                    {
                        Version = gitCommitDetails.Parents.FirstOrDefault(),
                        VersionType = GitVersionType.Commit,
                        VersionOptions = GitVersionOptions.None
                    }, resourceProvider);
                    
                    if (packageContent != null)
                    {
                        diagEntity = JsonConvert.DeserializeObject<DiagEntity>(packageContent);
                    }
                    else
                    {
                        throw new Exception("Package.json cannot be empty or null");
                    }
                    // Mark this detector as disabled. 
                    stringList.Add(new DevopsFileChange
                    {
                        CommitId = commitId,
                        Content = "",
                        PackageConfig = packageContent,
                        Path = change.Item.Path,
                        Metadata = "",
                        MarkAsDisabled = true,
                        Id = diagEntity.DetectorId
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
        private async Task<string> GetFileContentInCommit(Guid repoId, string ItemPath, GitVersionDescriptor gitVersionDescriptor, string resourceProviderType)
        {
            string content = string.Empty;
            await configDownloadTask;
            var streamResult = await defaultClient.GetItemContentAsync(repoId, ItemPath, null, VersionControlRecursionType.None, null,
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

            await configDownloadTask;
            ResourceProviderRepoConfig resourceProviderRepoConfig = await GetConfigbyResourceProvider(parameters.ResourceType);

            string gitFromdate;
            string gitToDate;
            CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(DataProviderConstants.DefaultTimeoutInSeconds));
            GitRepository repositoryAsync = await defaultClient.GetRepositoryAsync(resourceProviderRepoConfig.Project, resourceProviderRepoConfig.Repository, (object)null, tokenSource.Token);
            if (!string.IsNullOrWhiteSpace(parameters.FromCommitId) && !string.IsNullOrWhiteSpace(parameters.ToCommitId))
            {
                GitCommit fromCommitDetails = await defaultClient.GetCommitAsync(parameters.FromCommitId, repositoryAsync.Id);
                GitCommit toCommitDetails = await defaultClient.GetCommitAsync(parameters.ToCommitId, repositoryAsync.Id);
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
            List<GitCommitRef> commitsToProcess = await defaultClient.GetCommitsAsync(repositoryAsync.Id, new GitQueryCommitsCriteria
            {
                FromDate = gitFromdate,
                ToDate = gitToDate,
                ItemVersion = gitVersionDescriptor
            }, null, null, null, tokenSource.Token);
            foreach (var commit in commitsToProcess)
            {
                var filesinCommit = await GetFilesInCommit(commit.CommitId, parameters.ResourceType);
                // Process only the latest file update. 
                if (!result.Select(s => s.Id).Contains(filesinCommit.FirstOrDefault().Id))
                {
                    result.AddRange(filesinCommit);
                }

            }
            return result;
        }

        public async Task<ResourceProviderRepoConfig> GetRepoConfigsAsync(string resourceProviderType)
        {
            await configDownloadTask;
            ResourceProviderRepoConfig resourceMapping = await GetConfigbyResourceProvider(resourceProviderType);
           if(resourceMapping.ResourceProvider.Equals(resourceProviderType, StringComparison.CurrentCultureIgnoreCase))
           {
               return resourceMapping;
           }
            return null;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public async Task<List<GitPullRequest>> GetPRListAsync(string requestId, string resourceProviderType)
        {
            await configDownloadTask;
            ResourceProviderRepoConfig resourceProviderRepoConfig = await GetConfigbyResourceProvider(resourceProviderType);
            GitRepository repositoryAsync = await defaultClient.GetRepositoryAsync(resourceProviderRepoConfig.Project, resourceProviderRepoConfig.Repository, (object)null);

            GitPullRequestSearchCriteria gitPullRequestSearchCriteria = new GitPullRequestSearchCriteria();
            gitPullRequestSearchCriteria.IncludeLinks = true;
            gitPullRequestSearchCriteria.RepositoryId = repositoryAsync.Id;

            List<GitPullRequest> gitPullRequests =  await defaultClient.GetPullRequestsAsync(resourceProviderRepoConfig.Project, resourceProviderRepoConfig.Repository, gitPullRequestSearchCriteria );
            gitPullRequests.ForEach(element => element.RemoteUrl = $"{repositoryAsync.WebUrl}/pullrequest/{element.PullRequestId}");
            return gitPullRequests;
        }
    }
}
