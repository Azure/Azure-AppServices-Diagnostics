using Diagnostics.ModelsAndUtils.Models;
using System.Reflection;

namespace Diagnostics.RuntimeHost.Services.CacheService.Interfaces
{
    /// <summary>
    /// Interface for AssemblyCache Service.
    /// </summary>
    public interface IAssemblyCacheService
    {
        /// <summary>
        /// For a given <paramref name="assemblyName"/>, adds the <paramref name="assemblyDll"/> and <paramref name="compilerResponse"/> to the cache.
        /// If a limit of <see cref="MaxQueueSize"/> is reached, oldest assembly is removed
        /// </summary>
        /// <param name="assemblyName">Full Qualified name of the DLL to cache</param>
        /// <param name="assemblyDll">DLL to add to cache</param>
        void AddAssemblyToCache(string assemblyName, Assembly assemblyDll, CompilerResponse compilerResponse);

        /// <summary>
        /// Checks if a <paramref name="assemblyName"/> is loaded in cache
        /// </summary>
        bool IsAssemblyLoaded(string assemblyName);

        /// <summary>
        /// For the given <paramref name="assemblyName"/> fetches the cached compiler response
        /// </summary>
        /// <param name="assemblyName"></param>

        CompilerResponse GetCachedCompilerResponse(string assemblyName);

        /// <summary>
        /// For the given <paramref name="assemblyName"/> fetches the cached assembly 
        /// </summary>
        /// <param name="assemblyName"></param>
        Assembly GetCachedAssembly(string assemblyName);
    }
}
