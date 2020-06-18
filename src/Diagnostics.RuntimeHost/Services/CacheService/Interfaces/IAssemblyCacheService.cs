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
        /// Adds a given <paramref name="assemblyName"/> to the cache. If a limit of <see cref="MaxQueueSize"/> is reached, oldest assembly is removed
        /// </summary>
        /// <param name="assemblyName">Full Qualified name of the DLL to cache</param>
        /// <param name="assemblyDll">DLL to add to cache</param>
        void AddAssemblyToCache(string assemblyName, Assembly assemblyDll, CompilerResponse compilerResponse);

        /// <summary>
        /// Checks if a <paramref name="assemblyName"/> is loaded in cache and returns <paramref name="loadedAssembly"/>
        /// </summary>
        bool IsAssemblyLoaded(string assemblyName);


        CompilerResponse GetCachedCompilerResponse(string assemblyName);
        Assembly GetCachedAssembly(string assemblyName);
    }
}
