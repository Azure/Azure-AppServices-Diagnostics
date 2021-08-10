using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Diagnostics.Logger;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace Diagnostics.RuntimeHost.Services
{
    public interface IDevopsService
    {
       Task<List<DevopsFileChange>> GetFilesInCommit(string commitId);

        Task<List<DevopsFileChange>> GetFilesBetweenCommits(DeploymentParameters parameters);
    }
    public class DevopsService : IDevopsService
    {
        private IConfiguration globalConfig;
        private static GitHttpClient gitHttpClient;
        protected PartnerConfig partnerconfig;

        public DevopsService(IConfiguration configuration)
        {
            globalConfig = configuration;
        }

        public void InitializeClient(PartnerConfig config)
        {
            partnerconfig = config;
            gitHttpClient = DevopsClientFactory.GetDevopsClient(partnerconfig, globalConfig);
        }

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

        public async Task<List<DevopsFileChange>> GetFilesBetweenCommits(DeploymentParameters parameters)
        {
            var result = new List<DevopsFileChange>();
            
            // Caller has not provided any of the parameters, throw validation error.
            if (string.IsNullOrWhiteSpace(parameters.StartDate) && string.IsNullOrWhiteSpace(parameters.EndDate)
               && string.IsNullOrWhiteSpace(parameters.FromCommitId) && string.IsNullOrWhiteSpace(parameters.ToCommitId))
            {
                throw new ArgumentNullException("The given deployment parameters are invalid. Please provide both StartDate & EndDate or FromCommit & ToCommit");
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
            
            GitVersionDescriptor gitVersionDescriptor = new GitVersionDescriptor
            {
                VersionType = GitVersionType.Branch,
                Version = "master",
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
                //var filesinCommit = await GetFilesInCommit(commit.CommitId);
                //if(!result.Contains(filesinCommit.FirstOrDefault()))
                //{
                    result.AddRange(await GetFilesInCommit(commit.CommitId));
                //}
               
            }
            return result;
        }
    }
}
