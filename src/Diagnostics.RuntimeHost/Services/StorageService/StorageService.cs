using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Diagnostics.RuntimeHost.Services.StorageService
{
    public interface IStorageService
    {
        bool GetStorageFlag();
        Task<List<DiagEntity>> GetEntitiesByPartitionkey(string partitionKey = null, DateTime? startTime = null);
        Task<DiagEntity> LoadDataToTable(DiagEntity detectorEntity);
        Task<string> LoadBlobToContainer(string blobname, string contents);
        Task<byte[]> GetBlobByName(string name, string containerName = null);
        Task<int> ListBlobsInContainer();
        Task<DetectorRuntimeConfiguration> LoadConfiguration(DetectorRuntimeConfiguration configuration);
        Task<List<DetectorRuntimeConfiguration>> GetKustoConfiguration();
        Task LoadBatchDataToTable(List<DiagEntity> diagEntities);
        Task<byte[]> GetResourceProviderConfig();
    }
    public class StorageService : IStorageService
    {
        public static readonly string PartitionKey = "PartitionKey";
        public static readonly string RowKey = "RowKey";

        private static CloudTableClient tableClient;
        private static CloudBlobClient cloudBlobClient;
        private string tableName;
        private string container;
        private bool loadOnlyPublicDetectors;
        private bool isStorageEnabled;
        private string detectorRuntimeConfigTable;
        private string devopsConfigContainer;
        private string devopsConfigFile;

        public StorageService(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            tableName = configuration["SourceWatcher:TableName"];
            container = configuration["SourceWatcher:BlobContainerName"];
            detectorRuntimeConfigTable = configuration["SourceWatcher:DetectorRuntimeConfigTable"];
            devopsConfigContainer = configuration["SourceWatcher:DevOpsConfigContainer"];
            devopsConfigFile = configuration["SourceWatcher:DevOpsConfigFile"];
            if (hostingEnvironment != null && hostingEnvironment.EnvironmentName.Equals("UnitTest", StringComparison.CurrentCultureIgnoreCase))
            {
                tableClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient();
                cloudBlobClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudBlobClient();             
            }
            else
            {
                var accountname = configuration["SourceWatcher:DiagStorageAccount"];
                var key = configuration["SourceWatcher:DiagStorageKey"];
                var dnsSuffix = configuration["SourceWatcher:StorageDnsSuffix"];
                if (string.IsNullOrWhiteSpace(dnsSuffix))
                {
                    dnsSuffix = "core.windows.net";
                }
                var storageAccount = new CloudStorageAccount(new StorageCredentials(accountname, key), accountname, dnsSuffix, true);
                tableClient = storageAccount.CreateCloudTableClient();
                cloudBlobClient = storageAccount.CreateCloudBlobClient();             
            }

            if (!bool.TryParse((configuration[$"SourceWatcher:{RegistryConstants.LoadOnlyPublicDetectorsKey}"]), out loadOnlyPublicDetectors))
            {
                loadOnlyPublicDetectors = false;
            }
            var sourceWatcherType = Enum.Parse<SourceWatcherType>(configuration[$"SourceWatcher:{RegistryConstants.WatcherTypeKey}"]);
            if (sourceWatcherType.Equals(SourceWatcherType.AzureStorage))
            {
                isStorageEnabled = true;
            }

        }

        public async Task<List<DiagEntity>> GetEntitiesByPartitionkey(string partitionKey = null, DateTime? startTime = null)
        {
            int retryThreshold = 2;
            int attempt = 0;
            var detectorsResult = new List<DiagEntity>();
            do
            {
                var clientRequestId = Guid.NewGuid().ToString();
                try
                {
                    CloudTable table = tableClient.GetTableReference(tableName);
                    var timeTakenStopWatch = new Stopwatch();
                    if (string.IsNullOrWhiteSpace(partitionKey))
                    {
                        partitionKey = "Detector";
                    }
                    var filterPartitionKey = TableQuery.GenerateFilterCondition(PartitionKey, QueryComparisons.Equal, partitionKey);
                    DateTime timeFilter = startTime ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
                    string timestampFilter = TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThanOrEqual, new DateTimeOffset(timeFilter));
                    string finalFilter = timeFilter.Equals(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)) ? filterPartitionKey : TableQuery.CombineFilters(filterPartitionKey, TableOperators.And, timestampFilter);
                    var tableQuery = new TableQuery<DiagEntity>();
                    tableQuery.Where(finalFilter);
                    TableContinuationToken tableContinuationToken = null;
                    timeTakenStopWatch.Start();
                    TableRequestOptions tableRequestOptions = new TableRequestOptions();
                    tableRequestOptions.LocationMode = LocationMode.PrimaryThenSecondary;
                    tableRequestOptions.MaximumExecutionTime = TimeSpan.FromSeconds(30);
                    OperationContext oc = new OperationContext();
                    oc.ClientRequestID = clientRequestId;
                    if (attempt == retryThreshold)
                    {
                        tableRequestOptions.LocationMode = LocationMode.SecondaryOnly;
                        DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Retrying table against secondary account after {attempt} attempts");
                    }
                    do
                    {
                        DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Querying against {tableName} with Client Request id {oc.ClientRequestID}");
                        // Execute the operation.
                        var detectorList = await table.ExecuteQuerySegmentedAsync(tableQuery, tableContinuationToken, tableRequestOptions, null);
                        tableContinuationToken = detectorList.ContinuationToken;
                        if (detectorList.Results != null)
                        {
                            detectorsResult.AddRange(detectorList.Results);
                        }
                    } while (tableContinuationToken != null);
                    timeTakenStopWatch.Stop();
                    DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"GetEntities by Partition key {partitionKey} took {timeTakenStopWatch.ElapsedMilliseconds}, Total rows = {detectorsResult.Count}, ClientRequestId = {clientRequestId} ");
                     return startTime == DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc) ? detectorsResult.Where(result => !result.IsDisabled).ToList() :
                        detectorsResult.ToList();
                }
                catch (Exception ex)
                {
                    DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageService), $"ClientRequestId : {clientRequestId} {ex.Message}", ex.GetType().ToString(), ex.ToString());
                }
                finally
                {
                    attempt++;
                }
            } while (attempt <= retryThreshold);
            return detectorsResult;
        }

        public bool GetStorageFlag()
        {
            return isStorageEnabled;
        }

        public async Task<DiagEntity> LoadDataToTable(DiagEntity detectorEntity)
        {
            var clientRequestId = Guid.NewGuid().ToString();
            try
            {
                // Create a table client for interacting with the table service 
                CloudTable table =  tableClient.GetTableReference(tableName);
                if (detectorEntity == null || detectorEntity.PartitionKey == null || detectorEntity.RowKey == null)
                {
                    throw new ArgumentNullException(nameof(detectorEntity));
                }

                var timeTakenStopWatch = new Stopwatch();
                timeTakenStopWatch.Start();

                // Create the InsertOrReplace table operation
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(detectorEntity);
             
                TableRequestOptions tableRequestOptions = new TableRequestOptions();
                tableRequestOptions.LocationMode = LocationMode.PrimaryOnly;
                tableRequestOptions.MaximumExecutionTime = TimeSpan.FromSeconds(60);
                OperationContext oc = new OperationContext();
                oc.ClientRequestID = clientRequestId;
                // Execute the operation.

                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Insert or Replace {detectorEntity.RowKey} into {tableName} ClientRequestId {clientRequestId}");
                TableResult result = await table.ExecuteAsync(insertOrReplaceOperation, tableRequestOptions, oc);
                timeTakenStopWatch.Stop();
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"InsertOrReplace result : {result.HttpStatusCode}, time taken {timeTakenStopWatch.ElapsedMilliseconds}, ClientRequestId {clientRequestId}");
                DiagEntity insertedEntity = result.Result as DiagEntity;
                return detectorEntity;
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageService), $"ClientRequestId {clientRequestId} {ex.Message}", ex.GetType().ToString(), ex.ToString());
                return null;
            }
        }

        public async Task<string> LoadBlobToContainer(string blobname, string contents)
        {
            var clientRequestId = Guid.NewGuid().ToString();
            try
            {
                var timeTakenStopWatch = new Stopwatch();
                timeTakenStopWatch.Start();
                var containerReference = cloudBlobClient.GetContainerReference(container);
                var cloudBlob = containerReference.GetBlockBlobReference(blobname);
                BlobRequestOptions blobRequestOptions = new BlobRequestOptions();
                blobRequestOptions.LocationMode = LocationMode.PrimaryOnly;
                blobRequestOptions.MaximumExecutionTime = TimeSpan.FromSeconds(60);
                OperationContext oc = new OperationContext();
                oc.ClientRequestID = clientRequestId;
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Loading {blobname} with ClientRequestId {clientRequestId}");
                using (var uploadStream = new MemoryStream(Convert.FromBase64String(contents)))
                {
                    await cloudBlob.UploadFromStreamAsync(uploadStream, null, blobRequestOptions, oc);
                }
                await cloudBlob.FetchAttributesAsync();
                timeTakenStopWatch.Stop();
                var uploadResult = cloudBlob.Properties;
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Loaded {blobname}, etag {uploadResult.ETag}, time taken {timeTakenStopWatch.ElapsedMilliseconds} ClientRequestId {clientRequestId}");
                return uploadResult.ETag;
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageService), $" ClientRequestId {clientRequestId} {ex.Message}", ex.GetType().ToString(), ex.ToString());
                return null;
            }
        }

        public async Task<byte[]> GetBlobByName(string name, string containerName = null)
        {
            int retryThreshold = 2;
            int attempt = 0;
            do
            {
                var clientRequestId = Guid.NewGuid().ToString();
                try
                {

                    BlobRequestOptions options = new BlobRequestOptions();
                    options.LocationMode = LocationMode.PrimaryThenSecondary;
                    options.MaximumExecutionTime = TimeSpan.FromSeconds(30);
                    if (attempt == retryThreshold)
                    {
                        options.LocationMode = LocationMode.SecondaryOnly;
                        DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Retrying blob against secondary account after {attempt} attempts");
                    }
                    var timeTakenStopWatch = new Stopwatch();
                    timeTakenStopWatch.Start();
                    OperationContext oc = new OperationContext();
                    oc.ClientRequestID = clientRequestId;
                    DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Fetching blob {name} with ClientRequestid {clientRequestId}");
                    var containerReference = string.IsNullOrWhiteSpace(containerName) ? cloudBlobClient.GetContainerReference(container) :
                        cloudBlobClient.GetContainerReference(containerName);
                    var cloudBlob = containerReference.GetBlockBlobReference(name);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        await cloudBlob.DownloadToStreamAsync(ms, null, options, oc);
                        timeTakenStopWatch.Stop();
                        DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Downloaded {name} to memory stream, time taken {timeTakenStopWatch.ElapsedMilliseconds} ClientRequestid {clientRequestId}");
                        return ms.ToArray();
                    }
                                      
                }
                catch (Exception ex)
                {
                    DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageService), $"ClientRequestId {clientRequestId} {ex.Message}", ex.GetType().ToString(), ex.ToString());
                }
                finally
                {
                    attempt++;
                }
            } while (attempt <= retryThreshold);
            return null;
        }

        public async Task<int> ListBlobsInContainer()
        {
            var clientRequestId = Guid.NewGuid().ToString();
            try
            {
                var timeTakenStopWatch = new Stopwatch();
                timeTakenStopWatch.Start();
                BlobRequestOptions options = new BlobRequestOptions();
                options.LocationMode = LocationMode.PrimaryThenSecondary;
                options.MaximumExecutionTime = TimeSpan.FromSeconds(60);
                OperationContext oc = new OperationContext();
                oc.ClientRequestID = clientRequestId;
                var containerReference = cloudBlobClient.GetContainerReference(container);
                var blobsResult = await containerReference.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, 1000, null, options, oc);
                timeTakenStopWatch.Stop();
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Number of blobs stored in container {container} is {blobsResult.Results.Count()}, time taken {timeTakenStopWatch.ElapsedMilliseconds} milliseconds, ClientRequestId {clientRequestId}");
                return blobsResult.Results != null ? blobsResult.Results.Count() : 0;
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageService), $"ClientRequestId {clientRequestId} {ex.Message}", ex.GetType().ToString(), ex.ToString());
                throw ex;
            }
        }

        public async Task<DetectorRuntimeConfiguration> LoadConfiguration(DetectorRuntimeConfiguration configuration)
        {
            var clientRequestId = Guid.NewGuid().ToString();
            try
            {
                // Create a table client for interacting with the table service 
                CloudTable table = tableClient.GetTableReference(detectorRuntimeConfigTable);
                if (configuration == null || configuration.PartitionKey == null || configuration.RowKey == null)
                {
                    throw new ArgumentNullException(nameof(configuration));
                }
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Insert or Replace {configuration.RowKey} into {detectorRuntimeConfigTable} ClientRequestId {clientRequestId}");
                var timeTakenStopWatch = new Stopwatch();
                timeTakenStopWatch.Start();

                // Create the InsertOrReplace table operation
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(configuration);
                TableRequestOptions tableRequestOptions = new TableRequestOptions();
                tableRequestOptions.LocationMode = LocationMode.PrimaryOnly;
                tableRequestOptions.MaximumExecutionTime = TimeSpan.FromSeconds(60);
                OperationContext oc = new OperationContext();
                oc.ClientRequestID = clientRequestId;
                // Execute the operation.
                TableResult result = await table.ExecuteAsync(insertOrReplaceOperation, tableRequestOptions, oc);
                timeTakenStopWatch.Stop();
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"InsertOrReplace result : {result.HttpStatusCode}, time taken {timeTakenStopWatch.ElapsedMilliseconds} ClientRequestId {clientRequestId}");
                DetectorRuntimeConfiguration insertedRow = result.Result as DetectorRuntimeConfiguration;
                return insertedRow;
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageService), $"ClientRequestId {clientRequestId} {ex.Message}", ex.GetType().ToString(), ex.ToString());
                return null;
            }

        }

        public async Task<List<DetectorRuntimeConfiguration>> GetKustoConfiguration()
        {
            var clientRequestId = Guid.NewGuid().ToString();
            try
            {
                CloudTable cloudTable = tableClient.GetTableReference(detectorRuntimeConfigTable);
                var timeTakenStopWatch = new Stopwatch();
                var partitionkey = "KustoClusterMapping";
                var filterPartitionKey = TableQuery.GenerateFilterCondition(PartitionKey, QueryComparisons.Equal, partitionkey);
                var tableQuery = new TableQuery<DetectorRuntimeConfiguration>();
                tableQuery.Where(filterPartitionKey);
                TableContinuationToken tableContinuationToken = null;
                var diagConfigurationsResult = new List<DetectorRuntimeConfiguration>();
                timeTakenStopWatch.Start();
                do
                {
                    // Execute the operation.
                    TableRequestOptions tableRequestOptions = new TableRequestOptions();
                    tableRequestOptions.LocationMode = LocationMode.PrimaryThenSecondary;
                    tableRequestOptions.MaximumExecutionTime = TimeSpan.FromSeconds(30);
                    OperationContext oc = new OperationContext();
                    oc.ClientRequestID = clientRequestId;
                    DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Querying against table {detectorRuntimeConfigTable} with ClientRequestId {clientRequestId}");
                    var diagConfigurations = await cloudTable.ExecuteQuerySegmentedAsync(tableQuery, tableContinuationToken, tableRequestOptions, oc);
                    tableContinuationToken = diagConfigurations.ContinuationToken;
                    if (diagConfigurations.Results != null)
                    {
                        diagConfigurationsResult.AddRange(diagConfigurations.Results);
                    }
                } while (tableContinuationToken != null);
                timeTakenStopWatch.Stop();
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"GetConfiguration by Partition key {partitionkey} took {timeTakenStopWatch.ElapsedMilliseconds}, Total rows = {diagConfigurationsResult.Count} ClientRequestId {clientRequestId}");
                return diagConfigurationsResult.Where(row => !row.IsDisabled).ToList();
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageService), $"ClientRequestId {clientRequestId} {ex.Message}", ex.GetType().ToString(), ex.ToString());
                return null;
            }
        }

        public async Task LoadBatchDataToTable(List<DiagEntity> diagEntities)
        {
            var clientRequestId = Guid.NewGuid().ToString();
            try
            {
                // Create a table reference for interacting with the table service 
                CloudTable table = tableClient.GetTableReference(tableName);
                if (diagEntities == null || diagEntities.Count == 0)
                {
                    throw new ArgumentNullException("List is empty");
                }

                var timeTakenStopWatch = new Stopwatch();
                timeTakenStopWatch.Start();

                TableBatchOperation batchOperation = new TableBatchOperation();
                
                foreach (var entity in diagEntities)
                {
                    batchOperation.InsertOrReplace(entity);
                }
                
                TableRequestOptions tableRequestOptions = new TableRequestOptions();
                tableRequestOptions.LocationMode = LocationMode.PrimaryOnly;
                tableRequestOptions.MaximumExecutionTime = TimeSpan.FromSeconds(60);
                OperationContext oc = new OperationContext();
                oc.ClientRequestID = clientRequestId;
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Insert or Replace batch diag entities into {tableName} ClientRequestId {clientRequestId}");
                //Execute batch operation
                IList<TableResult> result = await table.ExecuteBatchAsync(batchOperation, tableRequestOptions, oc);            
                timeTakenStopWatch.Stop();
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"InsertOrReplace batch time taken {timeTakenStopWatch.ElapsedMilliseconds}, ClientRequestId {clientRequestId}");
                
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageService), $"ClientRequestId {clientRequestId} {ex.Message}", ex.GetType().ToString(), ex.ToString());
                throw;
            }
          
        }

        public async Task<byte[]> GetResourceProviderConfig()
        {
            return await GetBlobByName(devopsConfigFile, devopsConfigContainer);
        }
    }
}

