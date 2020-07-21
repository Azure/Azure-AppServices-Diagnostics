﻿using Diagnostics.DataProviders;
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
        private DateTime blobCacheLastModifiedTime;
        private IKustoMappingsCacheService kustoCacheService;
        private DateTime kustoCacheLastModifiedTime;
        private Task kustoConfigDownloadTask;

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
          

        public StorageWatcher(IHostingEnvironment env, IConfiguration config, IStorageService service, IInvokerCacheService invokerCache, IGistCacheService gistCache, IKustoMappingsCacheService kustoCache)
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
            kustoCacheService = kustoCache;
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
                // Insert Kusto Cluster Mapping to Configuration table.
                if(pkg.GetCommitContents().Any(content => content.FilePath.Contains("kustoClusterMappings", StringComparison.CurrentCultureIgnoreCase)))
                {
                    var kustoMappingPackage = pkg.GetCommitContents().Where(c => c.FilePath.Contains("kustoClusterMappings", StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                    var githubCommit = await gitHubClient.GetCommitByPath(kustoMappingPackage.FilePath);
                    // store kusto cluster data
                    var diagconfiguration = new DiagConfiguration{
                        PartitionKey = "KustoClusterMapping",
                        RowKey = kustoMappingPackage.FilePath.Split("/kustoClusterMappings.json")[0],
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
            kustoConfigDownloadTask = StartKustoCacheRefresh(true);
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
            kustoCacheLastModifiedTime = DateTime.UtcNow;
            do
            {
                await Task.Delay(_pollingIntervalInSeconds * 1000);
                await StartKustoCacheRefresh(false);
                kustoCacheLastModifiedTime = DateTime.UtcNow;
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
                entitiesToLoad.AddRange(filteredDetectors.Where(s => s.Timestamp >= blobCacheLastModifiedTime).ToList());
                entitiesToLoad.AddRange(gists.Where(s => s.Timestamp >= blobCacheLastModifiedTime).ToList());
            } else
            {
                entitiesToLoad.AddRange(filteredDetectors.ToList());
                entitiesToLoad.AddRange(gists);
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
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

                    // initializing Entry Point of Invoker using assembly
                    Assembly temp = Assembly.Load(assemblyData);
                    EntityType entityType = EntityType.Signal;
                    if (entity.PartitionKey.Equals("Gist"))
                    {
                        entityType = EntityType.Gist;
                    }
                    else if (entity.PartitionKey.Equals("Detector"))
                    {
                        entityType = EntityType.Detector;
                    }

                    var script = string.Empty;
                    if(entity.PartitionKey.Equals("Gist"))
                    {
                        script = await gitHubClient.GetFileContent($"{entity.RowKey.ToLower()}/{entity.RowKey.ToLower()}.csx");
                    }
                    EntityMetadata metaData = new EntityMetadata(script, entityType);
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

        private async Task StartKustoCacheRefresh(bool startup = false)
        {
            var diagConfigRows = await storageService.GetKustoConfiguration();
            var configsToLoad = new List<DiagConfiguration>();
            if(!startup)
            {
                configsToLoad.AddRange(diagConfigRows.Where(row => row.Timestamp >= kustoCacheLastModifiedTime).ToList());
            } else
            {
                configsToLoad.AddRange(diagConfigRows);
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                if (configsToLoad.Count > 0)
                {
                    DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageWatcher), $"Starting kusto config download to update cache, Number of configs to load: {configsToLoad.Count}, startup : {startup.ToString()} at {DateTime.UtcNow}");
                }
                foreach(var config in configsToLoad)
                {
                    var kustoMappingsStringContent = config.KustoClusterMapping;
                    var kustoMappings = (List<Dictionary<string, string>>)JsonConvert.DeserializeObject(kustoMappingsStringContent, typeof(List<Dictionary<string, string>>));
                    var resourceProvider = config.RowKey;
                    if (!kustoCacheService.ContainsKey(resourceProvider) || (kustoCacheService.TryGetValue(resourceProvider, out List<Dictionary<string, string>> value) && !value.Equals(kustoMappings)))
                    {
                        kustoCacheService.AddOrUpdate(resourceProvider, kustoMappings);
                    }
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
    }
}
