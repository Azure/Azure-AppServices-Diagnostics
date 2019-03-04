using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

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
        public GithubDetectorWorker(IInvokerCacheService invokerCache)
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
