using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;

namespace Diagnostics.RuntimeHost.Services
{
    public class AssemblyCacheService: IAssemblyCacheService
    {
        /// <summary>
        /// Queue to maintain top <see cref="MaxQueueSize"/> assemblies
        /// </summary>
        private ConcurrentQueue<string> AssemblyQueue;

        /// <summary>
        /// Dictionary to cache the assemblies
        /// </summary>
        private Dictionary<string, Assembly> AssemblyCache;

        /// <summary>
        /// Maximum number of Assemblies to maintain in cache
        /// </summary>
        const int MaxQueueSize = 100;

        /// <summary>
        /// Creates instance of AssemblyCacheService class and initializes the cache
        /// </summary>
        /// <param name="maxAssembliesCount">Maximum size of assemblies to maintain</param>
        public AssemblyCacheService()
        {
            AssemblyQueue = new ConcurrentQueue<string>();
            AssemblyCache = new Dictionary<string, Assembly>();
        }

        /// <summary>
        /// Adds a given <paramref name="assemblyName"/> to the cache. If a limit of <see cref="MaxQueueSize"/> is reached, oldest assembly is removed
        /// </summary>
        /// <param name="assemblyName">Full Qualified name of the DLL to cache</param>
        /// <param name="assemblyDll">DLL to add to cache</param>
        public void AddAssemblyToCache(string assemblyName, Assembly assemblyDll)
        {
            AssemblyCache.Add(assemblyName, assemblyDll);
            string oldAssembly = null;
            if(AssemblyQueue.Count == MaxQueueSize && AssemblyQueue.TryDequeue(out oldAssembly))
            {
                AssemblyCache.Remove(oldAssembly);
            }
            AssemblyQueue.Enqueue(assemblyName);
        }

        /// <summary>
        /// Checks if a <paramref name="assemblyName"/> is loaded in cache and returns <paramref name="loadedAssembly"/>
        /// </summary>
        public bool IsAssemblyLoaded(string assemblyName, out Assembly loadedAssembly)
        {
            loadedAssembly = null;
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                return false;
            }
            return AssemblyCache.TryGetValue(assemblyName, out loadedAssembly);
        }
    }
}
