using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
namespace Diagnostics.RuntimeHost.Services.StorageService
{
    public interface IStorageService
    {
        bool GetStorageFlag();
        Task<List<DiagEntity>> RetieveEntitiesByPartitionkey(string partitionKey = null);
        Task<DiagEntity> LoadDataToTable(DiagEntity detectorEntity);
    }
    public class StorageService : IStorageService
    {
        public static readonly string PartitionKey = "PartitionKey";
        public static readonly string RowKey = "RowKey";
        
        private static CloudTableClient tableClient;

        private string tableName;
        private bool loadOnlyPublicDetectors;
        private bool isStorageEnabled;

        public StorageService(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            tableName = configuration["SourceWatcher:TableName"];
            if(hostingEnvironment != null && hostingEnvironment.EnvironmentName.Equals("UnitTest", StringComparison.CurrentCultureIgnoreCase))
            {
                tableClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient();
            } else
            {
                var accountname = configuration["SourceWatcher:DiagStorageAccount"];
                var key = configuration["SourceWatcher:DiagStorageKey"];
                var storageAccount = new CloudStorageAccount(new StorageCredentials(accountname, key), accountname, "core.windows.net", true);
                tableClient = storageAccount.CreateCloudTableClient();
            }
         
            if (!bool.TryParse((configuration[$"SourceWatcher:{RegistryConstants.LoadOnlyPublicDetectorsKey}"]), out loadOnlyPublicDetectors))
            {
                loadOnlyPublicDetectors = false;
            }

            if(!bool.TryParse((configuration["SourceWatcher:UseStorageAsSource"]), out isStorageEnabled))
            {
                isStorageEnabled = false;
            }

            CheckTable();
        }


        private void CheckTable()
        {
            CloudTable table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExistsAsync();
        }

        public async Task<List<DiagEntity>> RetieveEntitiesByPartitionkey(string partitionKey = null)
        {
            try
            {
                CloudTable table = tableClient.GetTableReference(tableName);

                DiagnosticsETWProvider.Instance.LogSourceWatcherMessage(nameof(StorageService), $"Retrieving detectors from table");
                partitionKey = partitionKey == null ? "Detector" : partitionKey;
                var filterPartitionKey = TableQuery.GenerateFilterCondition(PartitionKey, QueryComparisons.Equal, partitionKey);
                var tableQuery = new TableQuery<DiagEntity>();
                if (partitionKey.Equals("Detector", StringComparison.CurrentCultureIgnoreCase))
                {
                    var conditionInternal = TableQuery.GenerateFilterConditionForBool("IsInternal", QueryComparisons.Equal, loadOnlyPublicDetectors);
                    var combinedFilter = TableQuery.CombineFilters(filterPartitionKey, TableOperators.And, conditionInternal);
                    tableQuery.Where(combinedFilter);
                } else
                {
                    tableQuery.Where(filterPartitionKey);
                }
                TableContinuationToken tableContinuationToken = null;
                var detectorsResult = new List<DiagEntity>();
                do
                {
                    // Execute the operation.
                    var detectorList = await table.ExecuteQuerySegmentedAsync(tableQuery, tableContinuationToken);
                    tableContinuationToken = detectorList.ContinuationToken;
                    if(detectorList.Results != null)
                    {
                        detectorsResult.AddRange(detectorList.Results);
                    }
                } while (tableContinuationToken != null);
              
                return detectorsResult;
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogSourceWatcherException(nameof(StorageService), ex.Message, ex.GetType().ToString(), ex.ToString());
                return null;
            }
        }

        public bool GetStorageFlag()
        {
            return isStorageEnabled;
        }

        public async Task<DiagEntity> LoadDataToTable(DiagEntity detectorEntity)
        {
            try
            {
                // Create a table client for interacting with the table service 
                CloudTable table = tableClient.GetTableReference(tableName);
                if (detectorEntity == null || detectorEntity.PartitionKey == null || detectorEntity.RowKey == null)
                {
                    throw new ArgumentNullException(nameof(detectorEntity));
                }

                DiagnosticsETWProvider.Instance.LogSourceWatcherMessage(nameof(StorageService), $"Insert or Replace {detectorEntity.RowKey} into {tableName}");
                // Create the InsertOrReplace table operation
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(detectorEntity);

                // Execute the operation.
                TableResult result = await table.ExecuteAsync(insertOrReplaceOperation);

                DiagnosticsETWProvider.Instance.LogSourceWatcherMessage(nameof(StorageService), $"InsertOrReplace result : {result.HttpStatusCode}");
                DiagEntity insertedCustomer = result.Result as DiagEntity;
                return detectorEntity;
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogSourceWatcherException(nameof(StorageService), ex.Message, ex.GetType().ToString(), ex.ToString());
                return null;
            }
        }

    }
}
