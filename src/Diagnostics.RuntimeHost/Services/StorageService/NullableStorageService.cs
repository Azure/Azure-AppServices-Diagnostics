using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models.Storage;

namespace Diagnostics.RuntimeHost.Services.StorageService
{
    public class NullableStorageService : IStorageService
    {
        public Task<byte[]> GetBlobByName(string name)
        {
            return Task.FromResult(new byte[1]);
        }

        public Task<List<DiagEntity>> GetEntitiesByPartitionkey(string partitionKey = null)
        {
            return Task.FromResult(new List<DiagEntity>());
        }

        public bool GetStorageFlag()
        {
            return false;
        }

        public Task<string> LoadBlobToContainer(string blobname, string contents)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<DiagEntity> LoadDataToTable(DiagEntity detectorEntity)
        {
            return Task.FromResult(new DiagEntity());
        }

        public async Task<int> ListBlobsInContainer()
        {
            return 0;
        }
    }
}
