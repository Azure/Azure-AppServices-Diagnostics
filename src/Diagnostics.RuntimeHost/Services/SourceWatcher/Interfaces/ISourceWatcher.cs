using Diagnostics.RuntimeHost.Models;
using System;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    /// <summary>
    /// Interface for source watcher.
    /// </summary>
    public interface ISourceWatcher
    {
        /// <summary>
        /// Start source watcher.
        /// </summary>
        void Start();

        /// <summary>
        /// Wait for iteration to complete.
        /// </summary>
        /// <returns></returns>
        Task WaitForFirstCompletion();

        /// <summary>
        /// Create or update package.
        /// </summary>
        /// <param name="pkg">The package.</param>
        /// <returns>Task for creating or updating package.</returns>
        Task CreateOrUpdatePackage(Package pkg);
    }
}
