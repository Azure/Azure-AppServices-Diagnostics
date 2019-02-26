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

        /// <summary>
        /// Create or update cache.
        /// </summary>
        /// <param name="subDir">Directory info</param>
        /// <returns>Task for adding item to cache.</returns
        public override async Task CreateOrUpdateCacheAsync(DirectoryInfo subDir)
        {
            try
            {
                var cacheId = await FileHelper.GetFileContentAsync(subDir.FullName, _cacheIdFileName);

                if (string.IsNullOrWhiteSpace(cacheId) || !InvokerCache.TryGetValue(cacheId, out EntityInvoker invoker))
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
                    if (mostRecentAssembly == default(FileInfo) || csxScriptFile == default(FileInfo))
                    {
                        LogWarning($"No Assembly file (.dll) or Csx File found (.csx). Skipping cache update");
                        return;
                    }

                    var scriptText = await FileHelper.GetFileContentAsync(csxScriptFile.FullName);
                    LogMessage($"Loading assembly : {mostRecentAssembly.FullName}");
                    var asm = Assembly.LoadFrom(mostRecentAssembly.FullName);
                    invoker = new EntityInvoker(new EntityMetadata(scriptText));
                    invoker.InitializeEntryPoint(asm);

                    if (invoker.EntryPointDefinitionAttribute != null)
                    {
                        LogMessage($"Updating cache with  new invoker with id : {invoker.EntryPointDefinitionAttribute.Id}");
                        InvokerCache.AddOrUpdate(invoker.EntryPointDefinitionAttribute.Id, invoker);
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
        /// <returns>Task for downloading and updating cache.</returns>
        public override async Task CreateOrUpdateCacheAsync(DirectoryInfo destDir, string scriptText, string assemblyPath)
        {
            LogMessage($"Loading assembly : {assemblyPath}");
            var asm = Assembly.LoadFrom(assemblyPath);

            var newInvoker = new EntityInvoker(new EntityMetadata(scriptText));
            newInvoker.InitializeEntryPoint(asm);

            var cacheIdFilePath = Path.Combine(destDir.FullName, _cacheIdFileName);

            // Remove the Old Invoker from Cache
            var lastCacheId = await FileHelper.GetFileContentAsync(cacheIdFilePath);
            if (!string.IsNullOrWhiteSpace(lastCacheId) && InvokerCache.TryRemoveValue(lastCacheId, out EntityInvoker oldInvoker))
            {
                LogMessage($"Removing old invoker with id : {oldInvoker.EntryPointDefinitionAttribute.Id} from Cache");
                oldInvoker.Dispose();
            }

            // Add new invoker to Cache and update Cache Id File
            if (newInvoker.EntryPointDefinitionAttribute != null)
            {
                LogMessage($"Updating cache with  new invoker with id : {newInvoker.EntryPointDefinitionAttribute.Id}");
                InvokerCache.AddOrUpdate(newInvoker.EntryPointDefinitionAttribute.Id, newInvoker);
                await FileHelper.WriteToFileAsync(cacheIdFilePath, newInvoker.EntryPointDefinitionAttribute.Id);
            }
            else
            {
                LogWarning("Missing Entry Point Definition attribute. skipping cache update");
            }
        }
    }
}
