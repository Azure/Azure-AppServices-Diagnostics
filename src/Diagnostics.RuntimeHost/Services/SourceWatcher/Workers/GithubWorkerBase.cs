using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Utilities;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher.Workers
{
    public abstract class GithubWorkerBase : IGithubWorker
    {
        protected readonly string _lastModifiedMarkerName = "_lastModified.marker";
        protected readonly string _deleteMarkerName = "_delete.marker";
        protected readonly string _cacheIdFileName = "cacheId.txt";
        protected readonly string _workerIdFileName = "workerId.txt";

        public abstract string Name { get; }

        public abstract Task CreateOrUpdateCacheAsync(DirectoryInfo subDir);
        public abstract Task CreateOrUpdateCacheAsync(IEnumerable<GithubEntry> githubEntries, DirectoryInfo artifactsDestination, string lastModifiedMarker);
        public abstract Task CreateOrUpdateCacheAsync(DirectoryInfo destDir, string lastModifiedMarker, string scriptText, string assemblyPath, string metadata);


        protected async Task<string> GetWorkerIdAsync(DirectoryInfo subDir)
        {
            var workerId = await FileHelper.GetFileContentAsync(subDir.FullName, _workerIdFileName);
            if (string.IsNullOrWhiteSpace(workerId))
            {
                return "DetectorWorker";
            }

            return workerId;
        }

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
