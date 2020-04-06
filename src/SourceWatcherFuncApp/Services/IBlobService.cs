using System;
using System.IO;
using Azure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure;

namespace SourceWatcherFuncApp.Services
{
    public interface IBlobService
    {
        void LoadBlobToContainer(string name, Stream uploadStream);
    }

    public class BlobService : IBlobService
    {
        private static BlobContainerClient cloudBobClient;

        private ILogger<BlobService> blobServiceLogger;

        private string containerName;

        private string storageUri;

        private Uri containerUri;

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
                blobServiceLogger.LogInformation($"Conflict occured for {name}, trying to delete and upload");
                var deleteOperation = await cloudBobClient.DeleteBlobAsync(name);
                blobServiceLogger.LogInformation($"Response from deleting {name} : {deleteOperation.Status}, {deleteOperation.ReasonPhrase}");
                if(deleteOperation.Status >= 200)
                {
                    blobServiceLogger.LogInformation($"Uploading {name} to blob");
                    var uploadResponse = await cloudBobClient.UploadBlobAsync(name, uploadStream);
                    blobServiceLogger.LogInformation($"Upload response for {name} : {uploadResponse.GetRawResponse().Status}, {uploadResponse.GetRawResponse().ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                blobServiceLogger.LogError(ex.ToString());
            }
        }
    }
}
