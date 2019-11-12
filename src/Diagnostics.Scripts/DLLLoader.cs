using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage;
using System.Threading.Tasks;

namespace Diagnostics.Scripts
{
    class DLLLoader
    {
        private static string storageConnectionString = "TODO";
        private static CloudBlobContainer cloudBlobContainer = null;
        private static CloudStorageAccount storageAccount;

        static DLLLoader()
        {
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                // If the connection string is valid, proceed with operations against Blob
                // storage here.

                // Create the CloudBlobClient that represents the 
                // Blob storage endpoint for the storage account.
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                // Create a container called 'detectors'
                cloudBlobContainer =
                    cloudBlobClient.GetContainerReference("detectors");
                if (!cloudBlobContainer.Exists())
                {
                    cloudBlobContainer.Create();
                    BlobContainerPermissions permissions = new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Off
                    };
                    cloudBlobContainer.SetPermissions(permissions);
                }
            }
            else
            {
                // Otherwise, let the user know that they need to define the environment variable.
                throw new Exception("Invalid storage account connection string");
            }
        }

        //public bool ValidCache { get; set; } = false;
        //public bool Loaded { get; set; } = false;
        public string Id { get; set; }

        public DLLLoader(string id)
        {
            Id = id;
        }

        public async Task<byte[]> LoadAsync()
        {
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(Id);
            byte[] ret = null;
            await cloudBlockBlob.DownloadToByteArrayAsync(ret, 0);
            return ret;
        }
    }
}
