using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models.Storage;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace Diagnostics.RuntimeHost.Services
{
    public interface IDevopsService
    {
       Task<List<DevopsFileChange>> GetFilesInCommit(string commitId);

        Task<List<DevopsFileChange>> GetFilesBetweenCommits(string fromCommitId, string toCommitId);
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
            foreach (GitChange change in changesAsync.Changes)
            {
                if (change.Item.Path.EndsWith(".csx") && (change.ChangeType == VersionControlChangeType.Add || change.ChangeType == VersionControlChangeType.Edit))
                {
                    // hack right now, ideally get from config
                    var detectorId = String.Join(";", Regex.Matches(change.Item.Path, @"\/(.+?)\/")
                                        .Cast<Match>()
                                        .Select(m => m.Groups[1].Value));

                    var gitversion = new GitVersionDescriptor{
                        Version = commitId,
                        VersionType = GitVersionType.Commit,
                        VersionOptions = GitVersionOptions.None
                    };               
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
                }
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

        public async Task<List<DevopsFileChange>> GetFilesBetweenCommits(string fromCommitId, string toCommitId)
        {
            var result = new List<DevopsFileChange>();
            if(string.IsNullOrWhiteSpace(fromCommitId) || string.IsNullOrWhiteSpace(toCommitId))
            {
                throw new ArgumentNullException($"{nameof(fromCommitId)} or {nameof(toCommitId)} cannot be empty");
            }

            GitRepository repositoryAsync = await gitHttpClient.GetRepositoryAsync(partnerconfig.Project, partnerconfig.Repository, (object)null, new CancellationToken());
            GitCommit fromCommitDetails = await gitHttpClient.GetCommitAsync(fromCommitId, repositoryAsync.Id);
            GitCommit toCommitDetails = await gitHttpClient.GetCommitAsync(toCommitId, repositoryAsync.Id);
            List<GitCommitRef> commitsToProcess = await gitHttpClient.GetCommitsAsync(repositoryAsync.Id, new GitQueryCommitsCriteria
            {
                FromDate = fromCommitDetails.Committer.Date.ToString(),
                ToDate = toCommitDetails.Committer.Date.ToString()
            });
            foreach (var commit in commitsToProcess)
            {
                result.AddRange(await GetFilesInCommit(commit.CommitId));
            }
            return result;
        }
    }
}
