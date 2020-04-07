using System;
using System.IO;
using Azure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure;
using System.Net.Http;
using Azure.Core.Pipeline;

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

        private static HttpClient httpClient = new HttpClient();

        public BlobService(IConfigurationRoot configuration, ILogger<BlobService> logger)
        {
            var accountname = configuration["AzureStorageAccount"];
            var key = configuration["AzureStorageKey"];
            containerName = configuration["BlobContainerName"];
            storageUri = $"https://{accountname}.blob.core.windows.net";
            containerUri = new Uri($"{storageUri}/{containerName}");
            StorageSharedKeyCredential credential = new StorageSharedKeyCredential(accountname, key);
            httpClient.Timeout = TimeSpan.FromSeconds(102);
            cloudBobClient = new BlobContainerClient(containerUri, credential, new BlobClientOptions
            {
               Transport = new HttpClientTransport(httpClient)
            });
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
    }
}
