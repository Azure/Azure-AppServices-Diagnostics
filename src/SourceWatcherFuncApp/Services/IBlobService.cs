using System;
using System.IO;
using Azure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure;
using System.Net.Http;
using Azure.Core.Pipeline;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.Storage.Blobs.Models;
using System.Net;

namespace SourceWatcherFuncApp.Services
{
    public interface IBlobService
    {
        void LoadBlobToContainer(string name, Stream uploadStream);
        Task<bool> CheckBlobExists(string blobName);
    }

    public class BlobService : IBlobService
    {
        private static BlobContainerClient cloudBobClient;

        private static HttpClient httpClient;

        private ILogger<BlobService> blobServiceLogger;

        private string containerName;

        private string storageUri;

        private Uri containerUri;
        
        private List<string> existingBlobs;

        public BlobService(IConfigurationRoot configuration, ILogger<BlobService> logger)
        {
            var accountname = configuration["AzureStorageAccount"];
            var key = configuration["AzureStorageKey"];
            containerName = configuration["BlobContainerName"];
            storageUri = $"https://{accountname}.blob.core.windows.net";
            containerUri = new Uri($"{storageUri}/{containerName}");
            StorageSharedKeyCredential credential = new StorageSharedKeyCredential(accountname, key);
            cloudBobClient = new BlobContainerClient(containerUri, credential);
            this.blobServiceLogger = logger ?? logger;
            existingBlobs = new List<string>();
        }

      
        public async void LoadBlobToContainer(string name, Stream uploadStream)
        {
            try
            {
                blobServiceLogger.LogInformation($"Uploading {name} to blob");
                var uploadResponse = await cloudBobClient.UploadBlobAsync(name, uploadStream);
                blobServiceLogger.LogInformation($"Upload response for {name} : {uploadResponse.GetRawResponse().Status}, {uploadResponse.GetRawResponse().ReasonPhrase}");
            }
            catch (RequestFailedException e) when (e.Status == 409)
            {
                // handle existing blob;
                blobServiceLogger.LogInformation($"Conflict occured for {name}, trying to override upload");
                var blobClient = cloudBobClient.GetBlobClient(name);
                var uploadExistingBlob = await blobClient.UploadAsync(uploadStream, true);
                blobServiceLogger.LogInformation($"Updated existing blob {name}, response: {uploadExistingBlob.GetRawResponse().Status}, {uploadExistingBlob.GetRawResponse().ReasonPhrase}");
            }
            catch (Exception ex)
            {
                blobServiceLogger.LogError(ex.ToString());
            }
        }

        public async Task<bool> CheckBlobExists(string blobName)
        {
            try
            {
                if(existingBlobs.Count < 1)
                {
                    // List all the blobs
                    var tasks = cloudBobClient.GetBlobsAsync().GetAsyncEnumerator();
                    while (await tasks.MoveNextAsync())
                    {
                        existingBlobs.Add(tasks.Current.Name);
                    }
                }

                return existingBlobs.Contains(blobName);            
            } catch (RequestFailedException e) 
            {
                return false;
            }
        }
    }
}
