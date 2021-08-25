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

        public Task<List<DiagEntity>> GetEntitiesByPartitionkey(string partitionKey = null, DateTime? startTime = null)
        {
            return Task.FromResult(new List<DiagEntity>());
        }

        public bool GetStorageFlag()
        {
            return false;
        }

        public Task<string> LoadBlobToContainer(string blobname, string contents, DetectorEnvironment environment = DetectorEnvironment.Prod)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<DiagEntity> LoadDataToTable(DiagEntity detectorEntity, DetectorEnvironment environment = DetectorEnvironment.Prod)
        {
            return Task.FromResult(new DiagEntity());
        }

        public async Task<int> ListBlobsInContainer()
        {
            return 0;
        }

        public Task<DetectorRuntimeConfiguration> LoadConfiguration(DetectorRuntimeConfiguration configuration)
        {
            return Task.FromResult(new DetectorRuntimeConfiguration());
        }

        public Task<List<DetectorRuntimeConfiguration>> GetKustoConfiguration()
        {
            return Task.FromResult(new List<DetectorRuntimeConfiguration>());
        }
    }
}
