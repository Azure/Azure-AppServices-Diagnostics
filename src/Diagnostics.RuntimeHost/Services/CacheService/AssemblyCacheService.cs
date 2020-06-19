using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;

namespace Diagnostics.RuntimeHost.Services.CacheService
{
    public class AssemblyCacheService : IAssemblyCacheService
    {
        /// <summary>
        /// Queue to maintain top <see cref="MaxQueueSize"/> assemblies
        /// </summary>
        private ConcurrentQueue<string> AssemblyQueue;

        /// <summary>
        /// Dictionary to cache the assemblies
        /// </summary>
        private Dictionary<string, CompilationCache> CompilationCache;

        /// <summary>
        /// Maximum number of Assemblies to maintain in cache
        /// </summary>
        private const int MaxQueueSize = 100;

        /// <summary>
        /// Creates instance of AssemblyCacheService class and initializes the cache
        /// </summary>
        /// <param name="maxAssembliesCount">Maximum size of assemblies to maintain</param>
        public AssemblyCacheService()
        {
            AssemblyQueue = new ConcurrentQueue<string>();
            CompilationCache = new Dictionary<string, CompilationCache>();
        }

        /// <summary>
        /// For a given <paramref name="assemblyName"/>, adds the <paramref name="assemblyDll"/> and <paramref name="compilerResponse"/> to the cache.
        /// If a limit of <see cref="MaxQueueSize"/> is reached, oldest assembly is removed
        /// </summary>
        /// <param name="assemblyName">Full Qualified name of the DLL to cache</param>
        /// <param name="assemblyDll">DLL to add to cache</param>
        public void AddAssemblyToCache(string assemblyName, Assembly assemblyDll, CompilerResponse compilerResponse)
        {
            CompilationCache.Add(assemblyName, new CompilationCache
            {
                AssemblyCache = assemblyDll,
                CompilerResponseCache = compilerResponse
            });
            string oldAssembly = null;
            if (AssemblyQueue.Count == MaxQueueSize && AssemblyQueue.TryDequeue(out oldAssembly))
            {
                CompilationCache.Remove(oldAssembly);
            }
            AssemblyQueue.Enqueue(assemblyName);
        }

        /// <summary>
        /// Checks if a <paramref name="assemblyName"/> is loaded in cache 
        /// </summary>
        public bool IsAssemblyLoaded(string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                return false;
            }
            return CompilationCache.TryGetValue(assemblyName, out CompilationCache compilationCache);
        }

        /// <summary>
        /// For the given <paramref name="assemblyName"/> fetches the cached compiler response
        /// </summary>
        /// <param name="assemblyName"></param>
        public CompilerResponse GetCachedCompilerResponse(string assemblyName)
        {
            if(CompilationCache.TryGetValue(assemblyName, out CompilationCache cacheItem))
            {
                return cacheItem.CompilerResponseCache;
            }
            return new CompilerResponse();
        }

        /// <summary>
        /// For the given <paramref name="assemblyName"/> fetches the cached assembly 
        /// </summary>
        /// <param name="assemblyName"></param>
        public Assembly GetCachedAssembly(string assemblyName)
        {
            if(CompilationCache.TryGetValue(assemblyName, out CompilationCache cacheItem))
            {
                return cacheItem.AssemblyCache;
            }
            return null;
        }
    }
}
