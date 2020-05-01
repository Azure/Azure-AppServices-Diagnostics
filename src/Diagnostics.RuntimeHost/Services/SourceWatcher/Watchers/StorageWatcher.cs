using Diagnostics.DataProviders;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services.StorageService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Newtonsoft.Json;
using Diagnostics.RuntimeHost.Utilities;
using System.IO;
using System.Text;
using Octokit;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher.Watchers
{
    public class StorageWatcher : ISourceWatcher
    {
        private IStorageService storageService;
        private IHostingEnvironment hostingEnvironment;
        private IConfiguration configuration;
        private IGithubClient gitHubClient;

        public StorageWatcher(IHostingEnvironment env, IConfiguration config, IStorageService service)
        {
            storageService = service;
            hostingEnvironment = env;
            configuration = config;
            gitHubClient = new GithubClient(env, config);
        }
        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task CreateOrUpdatePackage(Package pkg)
        {
            if (pkg == null)
            {
                throw new ArgumentNullException(nameof(pkg));
            }
            var blobName = $"{pkg.Id.ToLower()}/{pkg.Id.ToLower()}.dll";
            var etag = await storageService.LoadBlobToContainer(blobName, pkg.DllBytes);
            var gitCommit = await gitHubClient.GetCommitByPath(blobName);
            var diagEntity = JsonConvert.DeserializeObject<DiagEntity>(pkg.PackageConfig);
            if(gitCommit != null)
            {
                diagEntity.GitHubSha = gitCommit.Commit.Tree.Sha;
                diagEntity.GithubLastModified = gitCommit.Commit.Author.Date.DateTime.ToUniversalTime();
            }
            var assemblyData = new MemoryStream(Convert.FromBase64String(pkg.DllBytes));
            diagEntity = DiagEntityHelper.PrepareEntityForLoad(assemblyData, pkg.CodeString, diagEntity);
            await storageService.LoadDataToTable(diagEntity);
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public  Task WaitForFirstCompletion()
        {
            throw new NotImplementedException();
        }


    }
}
