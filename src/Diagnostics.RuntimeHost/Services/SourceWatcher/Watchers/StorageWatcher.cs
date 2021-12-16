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
using System.Diagnostics;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;

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
        private int _pollingIntervalInSeconds = 60;
        private DateTime blobCacheLastModifiedTime;
        private IKustoMappingsCacheService kustoMappingsCacheService;
        private DateTime kustoMappingsCacheLastModified;
        private Task kustoConfigDownloadTask;
        private IDiagEntityTableCacheService diagEntityTableCacheService;

        /// <summary>
        /// Using a flag incase anything goes wrong.
        /// </summary>
        private bool LoadGistFromRepo
        {
            get
            {
                if (bool.TryParse(configuration["LoadGistFromRepo"], out bool retval))
                {
                    return retval;
                }
                return false;
            }
        }

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
          

        public StorageWatcher(IHostingEnvironment env, IConfiguration config, IStorageService service, IInvokerCacheService invokerCache, 
                              IGistCacheService gistCache, IKustoMappingsCacheService kustoMappingsCache, IGithubClient githubClient, IDiagEntityTableCacheService tableCacheService)
        {
            storageService = service;
            hostingEnvironment = env;
            configuration = config;
            this.gitHubClient = githubClient;
            _invokerDictionary = new Dictionary<EntityType, ICache<string, EntityInvoker>>
            {
                { EntityType.Detector, invokerCache},
                { EntityType.Signal, invokerCache},
                { EntityType.Gist, gistCache}
            };
            kustoMappingsCacheService = kustoMappingsCache;
            diagEntityTableCacheService = tableCacheService;
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
                // Insert Kusto Cluster Mapping to Configuration table.
                if (pkg.GetCommitContents().Any(content => content.FilePath.Contains("kustoClusterMappings", StringComparison.CurrentCultureIgnoreCase)))
                {
                    var kustoMappingPackage = pkg.GetCommitContents().Where(c => c.FilePath.Contains("kustoClusterMappings", StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                    var githubCommit = await gitHubClient.GetCommitByPath(kustoMappingPackage.FilePath);
                    // store kusto cluster data
                    var diagconfiguration = new DetectorRuntimeConfiguration
                    {
                        PartitionKey = "KustoClusterMapping",
                        RowKey = pkg.Id.ToLower(),
                        GithubSha = githubCommit != null ? githubCommit.Commit.Tree.Sha : string.Empty,
                        KustoClusterMapping = kustoMappingPackage.Content
                    };
                    var insertedDiagConfig = await storageService.LoadConfiguration(diagconfiguration);
                    return;
                }
                var blobName = $"{pkg.Id.ToLower()}/{pkg.Id.ToLower()}.dll";
                var etag = await storageService.LoadBlobToContainer(blobName, pkg.DllBytes);
                if (string.IsNullOrWhiteSpace(etag))
                {
                    DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageWatcher), $"Uploading {pkg.Id} to blob failed, not proceeding further");
                    return;
                }
                var gitCommit = await gitHubClient.GetCommitByPath(blobName);
                var diagEntity = JsonConvert.DeserializeObject<DiagEntity>(pkg.PackageConfig);
                diagEntity.Metadata = pkg.Metadata;
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

                // Force refresh its own cache
                var assemblyData = Convert.FromBase64String(pkg.DllBytes);
                await UpdateInvokerCache(assemblyData, diagEntity.PartitionKey, diagEntity.RowKey, diagEntity.Metadata);
            } catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageWatcher), ex.Message, ex.GetType().ToString(), ex.ToString());
            }         
        }

        public void Start()
        {
            blobDowloadTask = StartBlobDownload(true);
            kustoConfigDownloadTask = StartKustoMappingsRefresh(true);
            StartPollingBlobChanges();
            StartPollingKustoConfigChanges();
        }

        private async Task StartPollingBlobChanges()
        {
            await blobDowloadTask;
            blobCacheLastModifiedTime = DateTime.UtcNow;
            do
            {
                await Task.Delay(_pollingIntervalInSeconds * 1000);
                await StartBlobDownload(false);
                blobCacheLastModifiedTime = DateTime.UtcNow;
            } while (true);
        }

        private async Task StartPollingKustoConfigChanges()
        {
            await kustoConfigDownloadTask;
            kustoMappingsCacheLastModified = DateTime.UtcNow;
            do
            {
                await Task.Delay(_pollingIntervalInSeconds * 1000);
                await StartKustoMappingsRefresh(false);
                kustoMappingsCacheLastModified = DateTime.UtcNow;
            } while (true);
        }

       
        private async Task StartBlobDownload(bool startup = false)
        {
            DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageWatcher), $"Blobcache last modified at {blobCacheLastModifiedTime}");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var entitiesToLoad = new List<DiagEntity>();
            try
            {
                var timeRange = DateTime.UtcNow.AddMinutes(-5);
                if(!diagEntityTableCacheService.TryGetValue("Detector", out List<DiagEntity> detectorsList) || detectorsList == null || detectorsList.Count < 1)
                {
                    detectorsList = await storageService.GetEntitiesByPartitionkey("Detector", startup ? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc) : timeRange);
                }
                var gists = new List<DiagEntity>();
                if (!LoadOnlyPublicDetectors && (!diagEntityTableCacheService.TryGetValue("Gist", out gists) || gists == null || gists.Count <1))
                {
                    gists = await storageService.GetEntitiesByPartitionkey("Gist", startup ? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc) : timeRange);
                } 
                var filteredDetectors = LoadOnlyPublicDetectors ? detectorsList.Where(row => !row.IsInternal).ToList() : detectorsList;
                if(startup)
                {
                    entitiesToLoad.AddRange(filteredDetectors);
                    entitiesToLoad.AddRange(gists);
                } else
                {
                    // Load cache with detectors published in last 5 minutes.
                    entitiesToLoad.AddRange(filteredDetectors.Where(s => s.Timestamp >= timeRange).ToList()); 
                    entitiesToLoad.AddRange(gists.Where(s => s.Timestamp >= timeRange).ToList());
                }
                
                if (entitiesToLoad.Count > 0)
                {
                    DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageWatcher), $"Starting blob download to update cache, Number of entities: {entitiesToLoad.Count}, startup : {startup.ToString()} at {DateTime.UtcNow}");
                }
                foreach (var entity in entitiesToLoad)
                {
                    var assemblyData = await storageService.GetBlobByName($"{entity.RowKey.ToLower()}/{entity.RowKey.ToLower()}.dll");
                    if (assemblyData == null || assemblyData.Length == 0)
                    {
                        DiagnosticsETWProvider.Instance.LogAzureStorageWarning(nameof(StorageWatcher), $" blob {entity.RowKey.ToLower()}.dll is either null or 0 bytes in length");
                        continue;
                    }
                    await UpdateInvokerCache(assemblyData, entity.PartitionKey, entity.RowKey, entity.Metadata);
                }           
            } 
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageWatcher), $"Exception occurred while trying to update invoker cache {ex.Message} ", ex.GetType().ToString(), ex.ToString());
            } 
            finally
            {
                stopwatch.Stop();
                if (entitiesToLoad.Count > 0)
                {
                    DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageWatcher), $"Blob download complete, Number of entities {entitiesToLoad.Count}, startup : {startup.ToString()} time ellapsed {stopwatch.ElapsedMilliseconds} millisecs");
                }
            }         
        }

        private async Task StartKustoMappingsRefresh(bool startup = false)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var configsToLoad = new List<DetectorRuntimeConfiguration>();      
            try
            {
                var diagConfigRows = await storageService.GetKustoConfiguration();
                if (!startup)
                {
                    configsToLoad.AddRange(diagConfigRows.Where(row => row.Timestamp >= kustoMappingsCacheLastModified).ToList());
                }
                else
                {
                    configsToLoad.AddRange(diagConfigRows);
                }
                if (configsToLoad.Count > 0)
                {
                    DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageWatcher), $"Starting kusto config download to update cache, Number of configs to load: {configsToLoad.Count}, startup : {startup.ToString()} at {DateTime.UtcNow}");
                }
                foreach(var config in configsToLoad)
                {
                    var kustoMappingsStringContent = config.KustoClusterMapping;
                    var kustoMappings = (List<Dictionary<string, string>>)JsonConvert.DeserializeObject(kustoMappingsStringContent, typeof(List<Dictionary<string, string>>));
                    var resourceProvider = config.RowKey;
                    DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageWatcher), $"Adding {resourceProvider} to kustoMapping cache");
                    kustoMappingsCacheService.AddOrUpdate(resourceProvider, kustoMappings);              
                }
            } 
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageWatcher), $"Exception occurred while trying to update kusto cache {ex.Message} ", ex.GetType().ToString(), ex.ToString());
            } finally
            {
                stopwatch.Stop();
                if (configsToLoad.Count > 0)
                {
                    DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageWatcher), $"Kusto config download complete, Number of configs {configsToLoad.Count}, startup : {startup.ToString()} time ellapsed {stopwatch.ElapsedMilliseconds} millisecs");
                }
            }
        }

        private async Task UpdateInvokerCache(byte[] assemblyData, string partitionkey, string rowkey, string metadata=null)
        {
            // initializing Entry Point of Invoker using assembly
            Assembly temp = Assembly.Load(assemblyData);
            EntityType entityType = EntityType.Signal;
            if (partitionkey.Equals("Gist"))
            {
                entityType = EntityType.Gist;
            }
            else if (partitionkey.Equals("Detector"))
            {
                entityType = EntityType.Detector;
            }

            var script = string.Empty;
            if (partitionkey.Equals("Gist") && !LoadGistFromRepo)
            {           
               script = await gitHubClient.GetFileContent($"{rowkey.ToLower()}/{rowkey.ToLower()}.csx"); 
            }
            EntityMetadata metaData = new EntityMetadata(script, entityType, metadata);
            var newInvoker = new EntityInvoker(metaData);
            newInvoker.InitializeEntryPoint(temp);

            if (_invokerDictionary.TryGetValue(entityType, out ICache<string, EntityInvoker> cache) && newInvoker.EntryPointDefinitionAttribute != null)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageWatcher), $"Updating cache with new invoker with id : {newInvoker.EntryPointDefinitionAttribute.Id} {partitionkey}");
                cache.AddOrUpdate(newInvoker.EntryPointDefinitionAttribute.Id, newInvoker);
            }
            else
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageWarning(nameof(StorageWatcher), $"No invoker cache exist for {entityType}");
            }
        }
    }
}
