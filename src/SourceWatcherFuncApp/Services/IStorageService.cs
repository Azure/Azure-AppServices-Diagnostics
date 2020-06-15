using System;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Logging;
using CloudStorageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.IO;
using Diagnostics.ModelsAndUtils.Models.Storage;

namespace SourceWatcherFuncApp.Services
{
    public interface IStorageService
    {
        Task<DiagEntity> LoadDataToTable(DiagEntity detectorEntity);
        Task<DiagEntity> GetEntityFromTable(string partitionKey, string rowKey);
        Task<bool> CheckDetectorExists(string currentDetector);
        void LoadBlobToContainer(string name, Stream uploadStream);
        Task<List<DiagEntity>> GetAllEntities();
    }

    public class StorageService : IStorageService
    {
        private static CloudTableClient tableClient;

        private static CloudBlobContainer blobContainer;
        
        private string blobContainerName;

        private string tableName;

        private ILogger<StorageService> storageServiceLogger;

        private List<string> existingDetectors;

        public static readonly string PartitionKey = "PartitionKey";

        public StorageService(IConfigurationRoot configuration, ILogger<StorageService> logger)
        {
            blobContainerName = configuration["BlobContainerName"];
            tableName = configuration["TableName"];
            var accountname = configuration["AzureStorageAccount"];
            var key = configuration["AzureStorageKey"];
            var containerUri = new Uri($"https://{accountname}.blob.core.windows.net/{blobContainerName}");
            var tableUri = new Uri($"https://{accountname}.table.core.windows.net/{tableName}");
            var storageAccount = new CloudStorageAccount(new StorageCredentials(accountname, key), accountname, "core.windows.net", true);
            tableClient = storageAccount.CreateCloudTableClient();
            blobContainer = new CloudBlobContainer(containerUri, new StorageCredentials(accountname, key));
            storageServiceLogger = logger ?? logger;
            existingDetectors = new List<string>();
        }

        public async Task<bool> CheckDetectorExists(string currentDetector)
        {
            try
            {
                if (existingDetectors.Count < 1)
                {
                    var blobsList = await blobContainer.ListBlobsSegmentedAsync(null);
                    foreach (var blobItem in blobsList.Results)
                    {
                        if(blobItem is CloudBlobDirectory)
                        {
                            var directory = (CloudBlobDirectory)blobItem;
                            var name = directory.Prefix.Replace("/", "");
                            existingDetectors.Add(name);
                        } 
                    }
                }
                return existingDetectors.Contains(currentDetector);
            } catch(Exception ex)
            {
                storageServiceLogger.LogError(ex.ToString());
                return false;
            }
        }

        public async void LoadBlobToContainer(string name, Stream uploadStream)
        {
            try
            {
                storageServiceLogger.LogInformation($"Uploading {name} blob");
                var cloudBlob = blobContainer.GetBlockBlobReference(name);
                await cloudBlob.UploadFromStreamAsync(uploadStream);
            } catch (Exception ex)
            {
                storageServiceLogger.LogError(ex.ToString());
            }    
        }
        public async Task<DiagEntity> LoadDataToTable(DiagEntity detectorEntity)
        {
            try { 
            // Create a table client for interacting with the table service 
            CloudTable table = tableClient.GetTableReference(tableName);
            if(detectorEntity == null || detectorEntity.PartitionKey == null || detectorEntity.RowKey == null)
            {
                throw new ArgumentNullException(nameof(detectorEntity));
            }

                storageServiceLogger.LogInformation($"Insert or Replace {detectorEntity.RowKey} into {tableName}");
                // Create the InsertOrReplace table operation
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(detectorEntity);

                // Execute the operation.
                TableResult result = await table.ExecuteAsync(insertOrReplaceOperation);

                storageServiceLogger.LogInformation($"InsertOrReplace result : {result.HttpStatusCode}");
                var insertedEntity = result.Result as DiagEntity;          
                return detectorEntity;
            }
            catch (Exception ex)
            {
                storageServiceLogger.LogError(ex.ToString());
                return null;
            }
        }

        public async Task<DiagEntity> GetEntityFromTable(string partitionKey, string rowKey)
        {
            try
            {
                CloudTable table = tableClient.GetTableReference(tableName);

                if(string.IsNullOrWhiteSpace(partitionKey) || string.IsNullOrWhiteSpace(rowKey))
                {
                    throw new ArgumentNullException($"{nameof(partitionKey)} or {nameof(rowKey)} is either null or empty");
                }

                storageServiceLogger.LogInformation($"Retrieving info from table for {rowKey}, {partitionKey}");
                TableOperation retrieveOperation = TableOperation.Retrieve<DiagEntity>(partitionKey, rowKey);
                // Execute the operation.
                TableResult result = await table.ExecuteAsync(retrieveOperation);
                var existingEntity = result.Result as DiagEntity;
                return existingEntity;
            } catch (Exception ex)
            {
                storageServiceLogger.LogError(ex.ToString());
                return null;
            }
        }

        public async Task<List<DiagEntity>> GetAllEntities()
        {
            try
            {
                CloudTable table = tableClient.GetTableReference(tableName);
                storageServiceLogger.LogInformation($"Retrieving all rows from {tableName}");
                var detectorFilter = TableQuery.GenerateFilterCondition(PartitionKey, QueryComparisons.Equal, "Detector");
                var gistFilter = TableQuery.GenerateFilterCondition(PartitionKey, QueryComparisons.Equal, "Gist");
                var combinedFilter = TableQuery.CombineFilters(detectorFilter, TableOperators.Or, gistFilter);
                var tableQuery = new TableQuery<DiagEntity>();
                tableQuery.Where(combinedFilter);
                TableContinuationToken tableContinuationToken = null;
                var allRows = new List<DiagEntity>();
                do
                {
                    // Execute the operation.
                    var detectorList = await table.ExecuteQuerySegmentedAsync(tableQuery, tableContinuationToken);
                    tableContinuationToken = detectorList.ContinuationToken;
                    if (detectorList.Results != null) 
                    {
                      allRows.AddRange(detectorList.Results);                      
                    }                           
                } while (tableContinuationToken != null);
                return allRows;
            }
            catch (Exception ex)
            {
                storageServiceLogger.LogError(ex.ToString());
                return null;
            }
        }

    }
}
