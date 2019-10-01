using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Converters;

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

        private readonly bool _loadOnlyPublicDetectors;

        public GithubWorkerBase(bool loadOnlyPublicDetectors)
        {
            _loadOnlyPublicDetectors = loadOnlyPublicDetectors;
        }

        private static Regex regexPublicDetectors = new Regex(@"InternalOnly\s*=\s*false",RegexOptions.IgnoreCase);

        /// <summary>
        /// Create or update cache.
        /// </summary>
        /// <param name="subDir">Directory info.</param>
        /// <returns>Task for adding item to cache.</returns>
        public virtual async Task CreateOrUpdateCacheAsync(DirectoryInfo subDir)
        {
            try
            {
                var cacheId = await FileHelper.GetFileContentAsync(subDir.FullName, _cacheIdFileName);

                var subDirModifiedMarker = await FileHelper.GetFileContentAsync(subDir.FullName, _lastModifiedMarkerName);

                if (string.IsNullOrWhiteSpace(cacheId) || !GetCacheService().TryGetValue(cacheId, out EntityInvoker invoker) || invoker.EntityMetadata.LastModifiedMarker != subDirModifiedMarker)
                {
                    LogMessage($"Folder : {subDir.FullName} missing in invoker cache.");

                    // Check if delete marker file exists.
                    var deleteMarkerFile = new FileInfo(Path.Combine(subDir.FullName, _deleteMarkerName));
                    if (deleteMarkerFile.Exists)
                    {
                        LogMessage("Folder marked for deletion. Skipping cache update");
                        return;
                    }

                    var mostRecentAssembly = GetMostRecentFileByExtension(subDir, ".dll");
                    var csxScriptFile = GetMostRecentFileByExtension(subDir, ".csx");
                    var metadataFile = Path.Combine(subDir.FullName, "metadata.json");
                    if (mostRecentAssembly == default(FileInfo) || csxScriptFile == default(FileInfo))
                    {
                        LogWarning("No Assembly file (.dll) or Csx File found (.csx). Skipping cache update");
                        return;
                    }

                    var scriptText = await FileHelper.GetFileContentAsync(csxScriptFile.FullName);

                    if (_loadOnlyPublicDetectors && !regexPublicDetectors.Match(scriptText).Success)
                    {
                        return;
                    }

                    var metadata = string.Empty;
                    if (File.Exists(metadataFile))
                    {
                        metadata = await FileHelper.GetFileContentAsync(metadataFile);
                    }

                    LogMessage($"Loading assembly : {mostRecentAssembly.FullName}");
                    var asm = Assembly.LoadFrom(mostRecentAssembly.FullName);
                    invoker = new EntityInvoker(new EntityMetadata(scriptText, GetEntityType(), metadata, subDirModifiedMarker));
                    invoker.InitializeEntryPoint(asm);

                    if (invoker.EntryPointDefinitionAttribute != null)
                    {
                        LogMessage($"Updating cache with new invoker with id : {invoker.EntryPointDefinitionAttribute.Id}");
                        GetCacheService().AddOrUpdate(invoker.EntryPointDefinitionAttribute.Id, invoker);
                        await FileHelper.WriteToFileAsync(subDir.FullName, _cacheIdFileName, invoker.EntryPointDefinitionAttribute.Id);
                    }
                    else
                    {
                        LogWarning("Missing Entry Point Definition attribute. skipping cache update");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Create or update cache.
        /// </summary>
        /// <param name="destDir">Destination directory.</param>
        /// <param name="scriptText">Script text.</param>
        /// <param name="assemblyPath">Assembly path.</param>
        /// <param name="metadata">Metadata</param>
        /// <returns>Task for downloading and updating cache.</returns>
        public async Task CreateOrUpdateCacheAsync(DirectoryInfo destDir, string lastModifiedMarker, string scriptText, string assemblyPath, string metadata)
        {
            if (_loadOnlyPublicDetectors && !regexPublicDetectors.Match(scriptText).Success)
            {
                return;
            }

            LogMessage($"Loading assembly : {assemblyPath}");
            var asm = Assembly.LoadFrom(assemblyPath);

            var newInvoker = new EntityInvoker(new EntityMetadata(scriptText, GetEntityType(), metadata, lastModifiedMarker));
            newInvoker.InitializeEntryPoint(asm);

            var cacheIdFilePath = Path.Combine(destDir.FullName, _cacheIdFileName);

            // Remove the Old Invoker from Cache
            var lastCacheId = await FileHelper.GetFileContentAsync(cacheIdFilePath);
            if (!string.IsNullOrWhiteSpace(lastCacheId) && GetCacheService().TryRemoveValue(lastCacheId, out EntityInvoker oldInvoker))
            {
                LogMessage($"Removing old invoker with id : {oldInvoker.EntryPointDefinitionAttribute.Id} from Cache");
                oldInvoker.Dispose();
            }

            // Add new invoker to Cache and update Cache Id File
            if (newInvoker.EntryPointDefinitionAttribute != null)
            {
                LogMessage($"Updating cache with  new invoker with id : {newInvoker.EntryPointDefinitionAttribute.Id}");
                GetCacheService().AddOrUpdate(newInvoker.EntryPointDefinitionAttribute.Id, newInvoker);
                await FileHelper.WriteToFileAsync(cacheIdFilePath, newInvoker.EntryPointDefinitionAttribute.Id);
            }
            else
            {
                LogWarning("Missing Entry Point Definition attribute. skipping cache update");
            }
        }

        protected abstract ICache<string, EntityInvoker> GetCacheService();

        protected abstract EntityType GetEntityType();

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
