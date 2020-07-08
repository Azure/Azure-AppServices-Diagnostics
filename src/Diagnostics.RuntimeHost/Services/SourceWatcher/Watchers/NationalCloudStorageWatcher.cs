using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Services.StorageService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher.Watchers
{
    public sealed class NationalCloudStorageWatcher : StorageWatcher
    {
        public NationalCloudStorageWatcher(IHostingEnvironment env, IConfiguration configuration, IStorageService service, IInvokerCacheService invokerCacheService, IGistCacheService gistCacheService): base(env, configuration, service, invokerCacheService, gistCacheService)
        {

        }

        public override Task CreateOrUpdatePackage(Package pkg)
        {
            return Task.CompletedTask;
        }
    }
}
