using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Diagnostics.RuntimeHost.Services.StorageService;
using Diagnostics.Logger;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace Diagnostics.RuntimeHost.Services
{
    /// <summary>
    /// Interface to Devops Service.
    /// </summary>
    public interface IDevopsService
    {
       Task<List<DevopsFileChange>> GetFilesInCommit(string commitId);

        Task<List<DevopsFileChange>> GetFilesBetweenCommits(DeploymentParameters parameters);

        Task<string> GetFileFromBranch(string filePath, string branch = "");
    }
    
    /// <summary>
    /// Devops Service methods using Azure Devops SDK
    /// </summary>
    public class DevopsService : IDevopsService
    {
        private IConfiguration globalConfig;
        private static GitHttpClient gitHttpClient;
        protected PartnerConfig partnerconfig;
        protected IStorageService storage;

        public DevopsService(IConfiguration configuration)
        {
            globalConfig = configuration;
            InitializeClient();     
        }

        public async void InitializeClient()
        {
            
            partnerconfig = new PartnerConfig
            {
                DevOpsUrl = "https://dev.azure.com/darreldonald",
                Project = "darreldonald-test-repo",
                FolderPath = "/",
                Repository = "darreldonald-test-repo",
                ResourceProvider = "Microsoft.Web/sites"
            };
            gitHttpClient = DevopsClientFactory.GetDevopsClient(partnerconfig, globalConfig);
        }

        /// <summary>
        /// Gets file change in the given commit.
        /// </summary>
        /// <param name="commitId">Commit id to process.</param>
        public async Task<List<DevopsFileChange>> GetFilesInCommit(string commitId)
        {
            if (string.IsNullOrWhiteSpace(commitId))
                throw new ArgumentNullException("commit id cannot be null");
            GitRepository repositoryAsync = await gitHttpClient.GetRepositoryAsync(partnerconfig.Project, partnerconfig.Repository, (object)null, new CancellationToken());
            GitCommitChanges changesAsync = await gitHttpClient.GetChangesAsync(commitId, repositoryAsync.Id);
            List<DevopsFileChange> stringList = new List<DevopsFileChange>();
            try
            {
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

                      
                        var detectorScriptContent = await GetFileContent(repositoryAsync.Id, change.Item.Path, gitversion);
                        var packageContent = await GetFileContent(repositoryAsync.Id, $"/{detectorId}/package.json", gitversion);
                        var metadataContent = await GetFileContent(repositoryAsync.Id, $"/{detectorId}/metadata.json", gitversion);
                        stringList.Add(new DevopsFileChange
                        {
                            CommitId = commitId,
                            Content = detectorScriptContent,
                            Path = change.Item.Path,
                            PackageConfig = packageContent,
                            Metadata = metadataContent
                        });
                    } else if (change.Item.Path.EndsWith(".csx") && (change.ChangeType == VersionControlChangeType.Delete))
                    {
                        var detectorId = String.Join(";", Regex.Matches(change.Item.Path, @"\/(.+?)\/")
                                           .Cast<Match>()
                                           .Select(m => m.Groups[1].Value));
                        
                        GitCommit gitCommitDetails = await gitHttpClient.GetCommitAsync(commitId, repositoryAsync.Id);
                        // Get the package.json from the parent commit since at this commit, the file doesn't exist.
                        var packageContent = await GetFileContent(repositoryAsync.Id, $"/{detectorId}/package.json", new GitVersionDescriptor
                        {
                            Version = gitCommitDetails.Parents.FirstOrDefault(),
                            VersionType = GitVersionType.Commit,
                            VersionOptions = GitVersionOptions.None
                        });
                        // Mark this detector as disabled. 
                        stringList.Add(new DevopsFileChange
                        {
                            CommitId = commitId,
                            Content= "",
                            PackageConfig = packageContent,
                            Path = change.Item.Path,
                            Metadata = "",
                            MarkAsDisabled = true
                        });
                    }
                }
            } catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Failed to get files in commit {ex.ToString()}");
            }
  
            return stringList;
        }

        /// <summary>
        /// Gets the file content as string for the given path and repo.
        /// </summary>
        /// <param name="repoId">Repo guid</param>
        /// <param name="ItemPath">Path of the item</param>
        /// <param name="gitVersionDescriptor">Git version descriptior</param>
        private async Task<string> GetFileContent(Guid repoId, string ItemPath, GitVersionDescriptor gitVersionDescriptor)
        {
            string content = string.Empty;
            var streamResult = await gitHttpClient.GetItemContentAsync(repoId, ItemPath, null, VersionControlRecursionType.None, null,
                       null, null, gitVersionDescriptor);
            using (var reader = new StreamReader(streamResult))
            {
                content = reader.ReadToEnd();
            }
            return content;
        }

        public async Task<string> GetFileFromBranch(string filePath, string branch = "")
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException($"{nameof(filePath)} cannot be empty");

            GitRepository repositoryAsync = await gitHttpClient.GetRepositoryAsync(partnerconfig.Project, partnerconfig.Repository, (object)null, new CancellationToken());


            if (string.IsNullOrWhiteSpace(branch))
            {
                branch = repositoryAsync.DefaultBranch.Replace("refs/heads/", "");
            }

            return await GetFileContent(repositoryAsync.Id, filePath, new GitVersionDescriptor
            {
                VersionType = GitVersionType.Branch,
                Version = branch,
                VersionOptions = GitVersionOptions.None
            });

        }

        /// <summary>
        /// Gets file changes to deploy between commits based on date range or commit range.
        /// </summary>
        /// <param name="parameters">The given deployment parameters</param>
        public async Task<List<DevopsFileChange>> GetFilesBetweenCommits(DeploymentParameters parameters)
        {
            var result = new List<DevopsFileChange>();
            
            // Caller has not provided any of the parameters, throw validation error.
            if (string.IsNullOrWhiteSpace(parameters.StartDate) && string.IsNullOrWhiteSpace(parameters.EndDate)
               && string.IsNullOrWhiteSpace(parameters.FromCommitId) && string.IsNullOrWhiteSpace(parameters.ToCommitId))
            {
                throw new ArgumentException("The given deployment parameters are invalid. Please provide both StartDate & EndDate or FromCommit & ToCommit");
            }
            // If both start date & End date are not given, throw validation error
            if((string.IsNullOrWhiteSpace(parameters.StartDate) && !string.IsNullOrWhiteSpace(parameters.EndDate))
                || (string.IsNullOrWhiteSpace(parameters.EndDate) && !string.IsNullOrWhiteSpace(parameters.StartDate)))
            {
                throw new ArgumentException("The given deployment parameters are invalid. Please provide both StartDate & EndDate");
            }
            // If both FromCommit & ToCommit are not given, throw validation error.
            if ((string.IsNullOrWhiteSpace(parameters.FromCommitId) && !string.IsNullOrWhiteSpace(parameters.ToCommitId))
                || (string.IsNullOrWhiteSpace(parameters.ToCommitId) && !string.IsNullOrWhiteSpace(parameters.FromCommitId)))
            {
                throw new ArgumentException("The given deployment parameters are invalid. Please provide both FromCommitId & ToCommitId");
            }
            string gitFromdate;
            string gitToDate;
            GitRepository repositoryAsync = await gitHttpClient.GetRepositoryAsync(partnerconfig.Project, partnerconfig.Repository, (object)null, new CancellationToken());
            if (!string.IsNullOrWhiteSpace(parameters.FromCommitId) && !string.IsNullOrWhiteSpace(parameters.ToCommitId))
            {
                GitCommit fromCommitDetails = await gitHttpClient.GetCommitAsync(parameters.FromCommitId, repositoryAsync.Id);
                GitCommit toCommitDetails = await gitHttpClient.GetCommitAsync(parameters.ToCommitId, repositoryAsync.Id);
                gitFromdate = fromCommitDetails.Committer.Date.ToString();
                gitToDate = toCommitDetails.Committer.Date.ToString();
            } else
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
            List<GitCommitRef> commitsToProcess = await gitHttpClient.GetCommitsAsync(repositoryAsync.Id, new GitQueryCommitsCriteria
            {
                FromDate = gitFromdate,
                ToDate = gitToDate,
                ItemVersion = gitVersionDescriptor
            });
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
    }
}
