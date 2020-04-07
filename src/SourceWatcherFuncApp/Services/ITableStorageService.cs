using System;
using Microsoft.Extensions.Configuration;
using SourceWatcherFuncApp.Entities;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Logging;
using CloudStorageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount;

namespace SourceWatcherFuncApp.Services
{
    public interface ITableStorageService
    {
        Task<DetectorEntity> LoadDataToTable(DetectorEntity detectorEntity);
        Task<DetectorEntity> GetEntityFromTable(string partitionKey, string rowKey);
    }

    public class TableStorageService : ITableStorageService
    {
        private static CloudTableClient tableClient;
        
        private string blobContainerName;

        private string tableName;

        private ILogger<TableStorageService> storageServiceLogger;

        public TableStorageService(IConfigurationRoot configuration, ILogger<TableStorageService> logger)
        {
            blobContainerName = configuration["BlobContainerName"];
            tableName = configuration["TableName"];
            var accountname = configuration["AzureStorageAccount"];
            var key = configuration["AzureStorageKey"];
            var containerUri = new Uri($"https://{accountname}.blob.core.windows.net/{blobContainerName}");
            var tableUri = new Uri($"https://{accountname}.table.core.windows.net/{tableName}");
            var storageAccount = new CloudStorageAccount(new StorageCredentials(accountname, key), accountname, "core.windows.net", true);
            tableClient = storageAccount.CreateCloudTableClient();
            storageServiceLogger = logger ?? logger;
        }
    
        public async Task<DetectorEntity> LoadDataToTable(DetectorEntity detectorEntity)
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
                DetectorEntity insertedCustomer = result.Result as DetectorEntity;          
                return detectorEntity;
            }
            catch (Exception ex)
            {
                storageServiceLogger.LogError(ex.ToString());
                return null;
            }
        }

        public async Task<DetectorEntity> GetEntityFromTable(string partitionKey, string rowKey)
        {
            try
            {
                CloudTable table = tableClient.GetTableReference(tableName);

                if(string.IsNullOrWhiteSpace(partitionKey) || string.IsNullOrWhiteSpace(rowKey))
                {
                    throw new ArgumentNullException($"{nameof(partitionKey)} or {nameof(rowKey)} is either null or empty");
                }

                storageServiceLogger.LogInformation($"Retrieving info from table for {rowKey}, {partitionKey}");
                TableOperation retrieveOperation = TableOperation.Retrieve<DetectorEntity>(partitionKey, rowKey);
                // Execute the operation.
                TableResult result = await table.ExecuteAsync(retrieveOperation);
                DetectorEntity existingEntity = result.Result as DetectorEntity;
                return existingEntity;
            } catch (Exception ex)
            {
                storageServiceLogger.LogError(ex.ToString());
                return null;
            }
        }

    }
}
