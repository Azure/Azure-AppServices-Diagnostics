using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher.Workers
{
    /// <summary>
    /// Detector worker.
    /// </summary>
    public class GithubDetectorWorker : GithubWorkerBase
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        public override string Name => "DetectorWorker";

        /// <summary>
        /// Gets invoker cache.
        /// </summary>
        public IInvokerCacheService InvokerCache { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GithubDetectorWorker"/> class.
        /// </summary>
        /// <param name="invokerCache">Invoker cache.</param>
        public GithubDetectorWorker(IInvokerCacheService invokerCache, bool loadOnlyPublicDetectors) : base(loadOnlyPublicDetectors)
        {
            InvokerCache = invokerCache;
        }

        protected override ICache<string, EntityInvoker> GetCacheService()
        {
            return InvokerCache;
        }

        protected override EntityType GetEntityType()
        {
            return EntityType.Signal;
        }
    }
}
