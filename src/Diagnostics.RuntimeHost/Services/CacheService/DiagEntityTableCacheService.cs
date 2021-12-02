using System.Collections.Generic;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;
using System.Collections.Concurrent;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Services.StorageService;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Diagnostics.RuntimeHost.Models;
using System.Linq;
using Diagnostics.Logger;
using System.Diagnostics;
using System;

namespace Diagnostics.RuntimeHost.Services.CacheService
{
    public class DiagEntityTableCacheService : IDiagEntityTableCacheService
    {
        private ConcurrentDictionary<string, List<DiagEntity>> cache;

        private IStorageService storageService;

        int cacheExpirationTimeInSecs = 60;

        private bool startUp = false;

        public DiagEntityTableCacheService(IStorageService service)
        {
            cache = new ConcurrentDictionary<string, List<DiagEntity>>();
            storageService = service;
            startUp = true;
            StartPolling();
        }

        private async void StartPolling()
        {      
            do
            {
                await Task.Delay(cacheExpirationTimeInSecs * 1000);
                
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(DiagEntityTableCacheService), "Start polling Azure Storage for refreshing cache");
                try
                {
                    var detectorTask = storageService.GetEntitiesByPartitionkey("Detector", startUp ? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc) : DateTime.UtcNow.AddMinutes(-5));
                    var gistTask = storageService.GetEntitiesByPartitionkey("Gist", startUp ? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc) : DateTime.UtcNow.AddMinutes(-5));
                    await Task.WhenAll(new Task[] { detectorTask, gistTask });
                    var detectorResult = await detectorTask;
                    if (startUp)
                    {
                        AddOrUpdate("Detector", detectorResult);
                    } else if (detectorResult.Count > 0)
                    {
                        UpdateCacheWithLatestEntities("Detector", detectorResult);
                    }
                    var gistResult = await gistTask;
                    if (startUp)
                    {
                        AddOrUpdate("Gist", gistResult);
                    } else if(gistResult.Count > 0)
                    {
                        UpdateCacheWithLatestEntities("Gist", gistResult);
                    }
                } catch (Exception ex)
                {
                    DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(DiagEntityTableCacheService), ex.Message, ex.GetType().ToString(), ex.ToString());
                } finally
                {
                    stopwatch.Stop();
                    startUp = false;
                    DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(DiagEntityTableCacheService), $"Polling completed, time taken {stopwatch.ElapsedMilliseconds} milliseconds");
                } 
            } while (true);      
        }

        public void AddOrUpdate(string key, List<DiagEntity> value)
        {
            cache.AddOrUpdate(key, value, (existingKey, existingValue) => value);
        }

        public bool ContainsKey(string key)
        {
            return cache.ContainsKey(key);
        }

        public IEnumerable<List<DiagEntity>> GetAll()
        {
            return cache.Values;
        }

        public bool TryGetValue(string key, out List<DiagEntity> value)
        {
            return cache.TryGetValue(key, out value);
        }

        public bool TryRemoveValue(string key, out List<DiagEntity> value)
        {
            return cache.TryRemove(key, out value);
        }

        public async Task<List<DiagEntity>> GetEntityListByType<TResource>(RuntimeContext<TResource> context, string entityType = null)
          where TResource : IResource
        {
            entityType = entityType == null ? "Detector" : entityType;

            var result = new List<DiagEntity>();

            while (!TryGetValue(entityType, out result))
            {
                var tableResult = await storageService.GetEntitiesByPartitionkey(entityType);
                if(tableResult != null)
                {
                    AddOrUpdate(entityType, tableResult);
                }         
            }

            result = result.Where(tableEntity => context.OperationContext.Resource.IsApplicable(tableEntity) && (context.ClientIsInternal || !tableEntity.IsInternal)).OrderBy(tableRow => tableRow.DetectorName).ToList();
            return result;
        }

        public bool IsStorageAsSourceEnabled()
        {
            return storageService.GetStorageFlag();
        }

        private void UpdateCacheWithLatestEntities(string key, List<DiagEntity> latestentities)
        {
            TryGetValue(key, out List<DiagEntity> existingEntities);
            latestentities.ForEach(element =>
            {
                int index = existingEntities.FindIndex(s => s.RowKey.Equals(element.RowKey, StringComparison.CurrentCultureIgnoreCase));
                if (index > -1)
                {
                    existingEntities.RemoveAt(index);
                }
            });
            existingEntities.AddRange(latestentities.Where(s => !s.IsDisabled));
            AddOrUpdate(key, existingEntities);
        }
    }
}
