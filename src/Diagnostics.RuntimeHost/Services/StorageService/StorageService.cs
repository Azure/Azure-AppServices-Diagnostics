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

namespace Diagnostics.RuntimeHost.Services.StorageService
{
    public interface IStorageService
    {
        bool GetStorageFlag();
        Task<List<DiagEntity>> GetEntitiesByPartitionkey(string partitionKey = null);
        Task<DiagEntity> LoadDataToTable(DiagEntity detectorEntity);
        Task<string> LoadBlobToContainer(string blobname, string contents);
        Task<byte[]> GetBlobByName(string name);

        Task<int> ListBlobsInContainer();
    }
    public class StorageService : IStorageService
    {
        public static readonly string PartitionKey = "PartitionKey";
        public static readonly string RowKey = "RowKey";
        
        private static CloudTableClient tableClient;
        private static CloudBlobContainer containerClient;
        private string tableName;
        private string container;
        private bool loadOnlyPublicDetectors;
        private bool isStorageEnabled;
        private CloudTable cloudTable;

        public StorageService(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            tableName = configuration["SourceWatcher:TableName"];
            container = configuration["SourceWatcher:BlobContainerName"];
            if(hostingEnvironment != null && hostingEnvironment.EnvironmentName.Equals("UnitTest", StringComparison.CurrentCultureIgnoreCase))
            {
                tableClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient();
                containerClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudBlobClient().GetContainerReference(container);
            } else
            {
                var accountname = configuration["SourceWatcher:DiagStorageAccount"];
                var key = configuration["SourceWatcher:DiagStorageKey"];
                var storageAccount = new CloudStorageAccount(new StorageCredentials(accountname, key), accountname, "core.windows.net", true);
                tableClient = storageAccount.CreateCloudTableClient();
                containerClient = storageAccount.CreateCloudBlobClient().GetContainerReference(container);
            }
         
            if (!bool.TryParse((configuration[$"SourceWatcher:{RegistryConstants.LoadOnlyPublicDetectorsKey}"]), out loadOnlyPublicDetectors))
            {
                loadOnlyPublicDetectors = false;
            }

            if(!bool.TryParse((configuration["SourceWatcher:UseStorageAsSource"]), out isStorageEnabled))
            {
                isStorageEnabled = false;
            }
            cloudTable = tableClient.GetTableReference(tableName);
        }

        public async Task<List<DiagEntity>> GetEntitiesByPartitionkey(string partitionKey = null)
        {
            try
            {
                CloudTable table = tableClient.GetTableReference(tableName);
                await table.CreateIfNotExistsAsync();
                var timeTakenStopWatch = new Stopwatch();             
                partitionKey = partitionKey == null ? "Detector" : partitionKey;
                var filterPartitionKey = TableQuery.GenerateFilterCondition(PartitionKey, QueryComparisons.Equal, partitionKey);
                var tableQuery = new TableQuery<DiagEntity>();
                tableQuery.Where(filterPartitionKey);
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"GetEntities by parition key {partitionKey}");
                TableContinuationToken tableContinuationToken = null;
                var detectorsResult = new List<DiagEntity>();
                timeTakenStopWatch.Start();
                do
                {
                    // Execute the operation.
                    var detectorList = await table.ExecuteQuerySegmentedAsync(tableQuery, tableContinuationToken);
                    tableContinuationToken = detectorList.ContinuationToken;
                    if (detectorList.Results != null)
                    {
                        detectorsResult.AddRange(detectorList.Results);
                    }
                } while (tableContinuationToken != null);
                timeTakenStopWatch.Stop();
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"GetEntities by Parition key {partitionKey} took {timeTakenStopWatch.ElapsedMilliseconds}");
                return detectorsResult.Where(result => !result.IsDisabled).ToList();
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageService), ex.Message, ex.GetType().ToString(), ex.ToString());
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
                await table.CreateIfNotExistsAsync();
                if (detectorEntity == null || detectorEntity.PartitionKey == null || detectorEntity.RowKey == null)
                {
                    throw new ArgumentNullException(nameof(detectorEntity));
                }

                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Insert or Replace {detectorEntity.RowKey} into {tableName}");
                var timeTakenStopWatch = new Stopwatch();
                timeTakenStopWatch.Start();
                    
                // Create the InsertOrReplace table operation
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(detectorEntity);

                // Execute the operation.
                TableResult result = await table.ExecuteAsync(insertOrReplaceOperation);
                timeTakenStopWatch.Stop();
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"InsertOrReplace result : {result.HttpStatusCode}, time taken {timeTakenStopWatch.ElapsedMilliseconds}");
                DiagEntity insertedCustomer = result.Result as DiagEntity;
                return detectorEntity;
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageService), ex.Message, ex.GetType().ToString(), ex.ToString());
                return null;
            }
        }

        public async Task<string> LoadBlobToContainer(string blobname, string contents)
        {
            try
            {
                var timeTakenStopWatch = new Stopwatch();
                await containerClient.CreateIfNotExistsAsync();       
                timeTakenStopWatch.Start();
                var cloudBlob = containerClient.GetBlockBlobReference(blobname);
                using (var uploadStream = new MemoryStream(Convert.FromBase64String(contents)))
                {
                    await cloudBlob.UploadFromStreamAsync(uploadStream);               
                }
                await cloudBlob.FetchAttributesAsync();
                timeTakenStopWatch.Stop();
                var uploadResult = cloudBlob.Properties;  
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Loaded {blobname}, etag {uploadResult.ETag}, time taken {timeTakenStopWatch.ElapsedMilliseconds}");
                return uploadResult.ETag;
            } catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageService), ex.Message, ex.GetType().ToString(), ex.ToString());
                return null;
            }
        }

        public async Task<byte[]> GetBlobByName(string name)
        {
            try
            {
                var timeTakenStopWatch = new Stopwatch();
                await containerClient.CreateIfNotExistsAsync();
                timeTakenStopWatch.Start();
                var cloudBlob = containerClient.GetBlockBlobReference(name);
                using (MemoryStream ms = new MemoryStream())
                {
                    await cloudBlob.DownloadToStreamAsync(ms);
                    timeTakenStopWatch.Stop();
                    DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Downloaded {name} to memory stream, time taken {timeTakenStopWatch.ElapsedMilliseconds}");
                    return ms.ToArray();
                }              
            } catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageService), ex.Message, ex.GetType().ToString(), ex.ToString());
                return null;
            }
        }
    
        public async Task<int> ListBlobsInContainer()
        {
            try
            {
                var timeTakenStopWatch = new Stopwatch();
                timeTakenStopWatch.Start();
                var blobsResult =  await containerClient.ListBlobsSegmentedAsync(null);
                timeTakenStopWatch.Stop();
                DiagnosticsETWProvider.Instance.LogAzureStorageMessage(nameof(StorageService), $"Number of blobs stored in container {container} is {blobsResult.Results.Count()}, time taken {timeTakenStopWatch.ElapsedMilliseconds} milliseconds");
                return blobsResult.Results != null ? blobsResult.Results.Count() : 0 ;
            } catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogAzureStorageException(nameof(StorageService), ex.Message, ex.GetType().ToString(), ex.ToString());
                throw ex;
            }
        }
    }
}
