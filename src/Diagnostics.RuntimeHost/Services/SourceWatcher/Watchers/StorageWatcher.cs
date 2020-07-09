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
using Diagnostics.RuntimeHost.Services.CacheService;
using System.Linq;
using System.Collections.Generic;
using Diagnostics.Scripts.Models;
using Diagnostics.Scripts;
using System.Reflection;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher.Watchers
{
    public class StorageWatcher : ISourceWatcher
    {

        public Task WaitForFirstCompletion() => blobDowloadTask;

        private IStorageService storageService;
        private IHostingEnvironment hostingEnvironment;
        private IConfiguration configuration;
        private IGithubClient gitHubClient;
        private Task blobDowloadTask;

        private Dictionary<EntityType, ICache<string, EntityInvoker>> _invokerDictionary;
        private int _pollingIntervalInSeconds = 30;
        private DateTime cacheLastModifiedTime;

        private bool LoadOnlyPublicDetectors
        {
            get
            {
                if (bool.TryParse(configuration["SourceWatcher:LoadOnlyPublicDetectors"], out bool retVal))
                {
                    return retVal;
                }

                return false;
            } 
        }
          

        public StorageWatcher(IHostingEnvironment env, IConfiguration config, IStorageService service, IInvokerCacheService invokerCache, IGistCacheService gistCache)
        {
            storageService = service;
            hostingEnvironment = env;
            configuration = config;
            gitHubClient = new GithubClient(env, config);
            _invokerDictionary = new Dictionary<EntityType, ICache<string, EntityInvoker>>
            {
                { EntityType.Detector, invokerCache},
                { EntityType.Signal, invokerCache},
                { EntityType.Gist, gistCache}
            };
            Start();
        }

        public virtual async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            HealthCheckResult result = null;
            Exception storageException = null;
            try
            {
                var response = await storageService.ListBlobsInContainer();
            }
            catch (Exception ex)
            {
                storageException = ex;
            }
            finally
            {
                result = new HealthCheckResult(storageException == null ? HealthStatus.Healthy : HealthStatus.Unhealthy, "Azure Storage", "Run a test against azure storage by listing blobs in container", storageException);
            }
            return result;
        }

        public virtual async Task CreateOrUpdatePackage(Package pkg)
        {
            if (pkg == null)
            {
                throw new ArgumentNullException(nameof(pkg));
            }
            try
            {
                await gitHubClient.CreateOrUpdateFiles(pkg.GetCommitContents(), pkg.GetCommitMessage());
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
                    diagEntity.GithubLastModified = gitCommit.Commit.Author.Date.LocalDateTime; // Setting it as local date time because storage sdk converts to UTC while saving
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
            blobDowloadTask = StartBlobDownload(true);
            StartPollingForChanges();
        }

        private async Task StartPollingForChanges()
        {
            await blobDowloadTask;
            DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageWatcher), $"Start up blob download task completed at {DateTime.UtcNow}");
            cacheLastModifiedTime = DateTime.UtcNow;
            do
            {
                await Task.Delay(_pollingIntervalInSeconds * 1000);
                await StartBlobDownload(false);
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageWatcher), $"Polling for blob download task completed at {DateTime.UtcNow}");
                cacheLastModifiedTime = DateTime.UtcNow;
            } while (true);
        }

       
        private async Task StartBlobDownload(bool startup = false)
        {
            var detectorsList = await storageService.GetEntitiesByPartitionkey("Detector");
            var gists = await storageService.GetEntitiesByPartitionkey("Gist");
            var entitiesToLoad = new List<DiagEntity>();
            var filteredDetectors = LoadOnlyPublicDetectors ? detectorsList.Where(row => !row.IsInternal).ToList() : detectorsList;
            if(!startup)
            {
                entitiesToLoad.AddRange(filteredDetectors.Where(s => s.Timestamp >= cacheLastModifiedTime).ToList());
                entitiesToLoad.AddRange(gists.Where(s => s.Timestamp >= cacheLastModifiedTime).ToList());
            } else
            {
                entitiesToLoad.AddRange(filteredDetectors.ToList());
                entitiesToLoad.AddRange(gists);
            }

            try
            {
                foreach (var entity in entitiesToLoad)
                {
                    var assemblyData = await storageService.GetBlobByName($"{entity.RowKey.ToLower()}/{entity.RowKey.ToLower()}.dll");
                    if (assemblyData == null || assemblyData.Length == 0)
                    {
                        DiagnosticsETWProvider.Instance.LogAzureStorageWarning(nameof(StorageWatcher), $" blob {entity.RowKey.ToLower()}.dll is either null or 0 bytes in length");
                        continue;
                    }

                    // initializing Entry Point of Invoker using assembly
                    Assembly temp = Assembly.Load(assemblyData);
                    EntityType entityType = EntityType.Signal;
                    if(entity.PartitionKey.Equals("Gist"))
                    {
                        entityType = EntityType.Gist;
                    }
                    else if (entity.PartitionKey.Equals("Detector"))
                    {
                        entityType = EntityType.Detector;
                    }
                    EntityMetadata metaData = new EntityMetadata(string.Empty, entityType);
                    var newInvoker = new EntityInvoker(metaData);
                    newInvoker.InitializeEntryPoint(temp);

                    if (_invokerDictionary.TryGetValue(entityType, out ICache<string, EntityInvoker> cache) && newInvoker.EntryPointDefinitionAttribute != null)
                    {
                        DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageWatcher), $"Updating cache with new invoker with id : {newInvoker.EntryPointDefinitionAttribute.Id}");
                        cache.AddOrUpdate(newInvoker.EntryPointDefinitionAttribute.Id, newInvoker);
                    }
                    else
                    {
                        DiagnosticsETWProvider.Instance.LogAzureStorageWarning(nameof(StorageWatcher), $"No invoker cache exist for {entityType}");
                    }
                }
            } catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageWatcher), $"Exception occurred while trying to update cache {ex.Message} ", ex.GetType().ToString(), ex.ToString());
            }
            
        }

    }
}
