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

namespace Diagnostics.RuntimeHost.Services
{
    public interface IDevopsService
    {
       Task<List<DevopsFileChange>> GetFilesInCommit(string commitId);
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
                    string content = string.Empty;
                    var gitversion = new GitVersionDescriptor{
                        Version = commitId,
                        VersionType = GitVersionType.Commit,
                        VersionOptions = GitVersionOptions.None
                    };
                    var streamResult = await gitHttpClient.GetItemContentAsync(repositoryAsync.Id, change.Item.Path, null, VersionControlRecursionType.None, null,
                        null, null, gitversion);
                    using (var reader = new StreamReader(streamResult))
                    {
                        content = reader.ReadToEnd();
                    }
                    stringList.Add(new DevopsFileChange
                    {
                        CommitId = commitId,
                        Content = content,
                        Path = change.Item.Path
                    }); 
                }
            }
            return stringList;
        }
    }
}
