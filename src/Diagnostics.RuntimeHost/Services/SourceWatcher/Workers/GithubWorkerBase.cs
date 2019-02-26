using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher.Workers
{
    /// <summary>
    /// Github worker base class.
    /// </summary>
    public abstract class GithubWorkerBase : IGithubWorker
    {
        /// <summary>
        /// Worker name.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Create or update cache.
        /// </summary>
        /// <param name="subDir">Directory info.</param>
        /// <returns>Task for adding item to cache.</returns>
        public abstract Task CreateOrUpdateCacheAsync(DirectoryInfo subDir);

        /// <summary>
        /// Create or update cache.
        /// </summary>
        /// <param name="destDir">Destination directory.</param>
        /// <param name="scriptText">Script text.</param>
        /// <param name="assemblyPath">Assembly path.</param>
        /// <returns>Task for downloading and updating cache.</returns>
        public abstract Task CreateOrUpdateCacheAsync(DirectoryInfo destDir, string scriptText, string assemblyPath);

        protected readonly string _lastModifiedMarkerName = "_lastModified.marker";
        protected readonly string _deleteMarkerName = "_delete.marker";
        protected readonly string _cacheIdFileName = "cacheId.txt";

        protected static void LogMessage(string message)
        {
            DiagnosticsETWProvider.Instance.LogSourceWatcherMessage("GithubWatcher", message);
        }

        protected static void LogWarning(string message)
        {
            DiagnosticsETWProvider.Instance.LogSourceWatcherWarning("GithubWatcher", message);
        }

        protected static void LogException(string message, Exception ex)
        {
            var exception = new SourceWatcherException("Github", message, ex);
            DiagnosticsETWProvider.Instance.LogSourceWatcherException("GithubWatcher", message, exception.GetType().ToString(), exception.ToString());
        }

        protected static FileInfo GetMostRecentFileByExtension(DirectoryInfo dir, string extension)
        {
            return dir.GetFiles().Where(p => (!string.IsNullOrWhiteSpace(p.Extension) && p.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
        }
    }
}
