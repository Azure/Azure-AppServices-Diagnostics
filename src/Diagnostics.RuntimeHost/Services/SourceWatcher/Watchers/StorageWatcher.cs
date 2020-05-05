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
using Diagnostics.Logger;

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
            try
            {
                var blobName = $"{pkg.Id.ToLower()}/{pkg.Id.ToLower()}.dll";
                var etag = await storageService.LoadBlobToContainer(blobName, pkg.DllBytes);
                if (string.IsNullOrWhiteSpace(etag))
                {
                    DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageWatcher), $"Uploading {pkg.Id} to blob failed, not proceeding further");
                    return;
                }
                var gitCommit = await gitHubClient.GetCommitByPath(blobName);
                var diagEntity = JsonConvert.DeserializeObject<DiagEntity>(pkg.PackageConfig);
                if (gitCommit != null)
                {
                    diagEntity.GitHubSha = gitCommit.Commit.Tree.Sha;
                    diagEntity.GithubLastModified = gitCommit.Commit.Author.Date.DateTime.ToUniversalTime();
                }
                using (var ms = new MemoryStream(Convert.FromBase64String(pkg.DllBytes)))
                {
                    var assemblyBytes = DiagEntityHelper.GetByteFromStream(ms);
                    diagEntity = DiagEntityHelper.PrepareEntityForLoad(assemblyBytes, pkg.CodeString, diagEntity);
                }
                await storageService.LoadDataToTable(diagEntity);
            } catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageWatcher), ex.Message, ex.GetType().ToString(), ex.ToString());
            }         
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
