using System.IO;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher.Workers
{
    /// <summary>
    /// Interface for github operation.
    /// </summary>
    public interface IGithubWorker
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Create or update cache.
        /// </summary>
        /// <param name="subDir">Directory info.</param>
        /// <returns>Task for adding item to cache.</returns>
        Task CreateOrUpdateCacheAsync(DirectoryInfo subDir);

        /// <summary>
        /// Create or update cache.
        /// </summary>
        /// <param name="destDir">Destination directory.</param>
        /// <param name="scriptText">Script text.</param>
        /// <param name="assemblyPath">Assembly path.</param>
        /// <returns>Task for downloading and updating cache.</returns>
        Task CreateOrUpdateCacheAsync(DirectoryInfo destDir, string scriptText, string assemblyPath, string metadata);
    }
}
