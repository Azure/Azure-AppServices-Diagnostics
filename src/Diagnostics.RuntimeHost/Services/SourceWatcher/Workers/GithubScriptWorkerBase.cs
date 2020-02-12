using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher.Workers
{
    /// <summary>
    /// Base class for gists and detector github workers.
    /// </summary>
    public abstract class GithubScriptWorkerBase : GithubWorkerBase
    {
        private readonly bool _loadOnlyPublicDetectors;
        private IGithubClient _githubClient;
        private static Regex regexPublicDetectors = new Regex(@"InternalOnly\s*=\s*false", RegexOptions.IgnoreCase);

        public GithubScriptWorkerBase(bool loadOnlyPublicDetectors, IGithubClient githubClient)
        {
            _loadOnlyPublicDetectors = loadOnlyPublicDetectors;
            _githubClient = githubClient;
        }

        protected abstract ICache<string, EntityInvoker> GetCacheService();

        protected abstract EntityType GetEntityType();

        /// <summary>
        /// Create or update cache.
        /// </summary>
        /// <param name="subDir">Directory info.</param>
        /// <returns>Task for adding item to cache.</returns>
        public override async Task CreateOrUpdateCacheAsync(DirectoryInfo subDir)
        {
            try
            {
                if (!(await GetWorkerIdAsync(subDir)).Equals(this.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    return;
                }

                var cacheId = await FileHelper.GetFileContentAsync(subDir.FullName, _cacheIdFileName);

                var subDirSha = await FileHelper.GetFileContentAsync(subDir.FullName, _lastModifiedMarkerName);

                if (string.IsNullOrWhiteSpace(cacheId) || !GetCacheService().TryGetValue(cacheId, out EntityInvoker invoker) || invoker.EntityMetadata.Sha != subDirSha)
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
                    invoker = new EntityInvoker(new EntityMetadata(scriptText, GetEntityType(), metadata, subDirSha));
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

        public override async Task CreateOrUpdateCacheAsync(IEnumerable<GithubEntry> githubEntries, DirectoryInfo artifactsDestination, string lastModifiedMarker)
        {
            var assemblyName = Guid.NewGuid().ToString();
            var csxFilePath = string.Empty;
            var confFilePath = string.Empty;
            var metadataFilePath = string.Empty;
            var expectedFiles = new string[] { "csx", "package.json", "metadata.json" };

            if (!githubEntries.Any(x => expectedFiles.Any(y => x.Name.Contains(y, StringComparison.CurrentCultureIgnoreCase))))
            {
                return;
            }

            foreach (GithubEntry githubFile in githubEntries)
            {
                var fileExtension = githubFile.Name.Split(new char[] { '.' }).LastOrDefault();

                var downloadFilePath = Path.Combine(artifactsDestination.FullName, githubFile.Name.ToLower());

                if (fileExtension.Equals("csx", StringComparison.OrdinalIgnoreCase))
                {
                    csxFilePath = downloadFilePath;
                }
                else if (githubFile.Name.Equals("package.json", StringComparison.OrdinalIgnoreCase))
                {
                    confFilePath = downloadFilePath;
                }
                else if (githubFile.Name.Equals("metadata.json", StringComparison.OrdinalIgnoreCase))
                {
                    metadataFilePath = downloadFilePath;
                }
                else
                {
                    // Use Guids for Assembly and PDB Names to ensure uniqueness.
                    downloadFilePath = Path.Combine(artifactsDestination.FullName, $"{assemblyName}.{fileExtension.ToLower()}");
                }

                LogMessage($"Begin downloading File : {githubFile.Name.ToLower()} and saving it as : {downloadFilePath}");
                await _githubClient.DownloadFile(githubFile.Download_url, downloadFilePath);
            }

            var scriptText = await FileHelper.GetFileContentAsync(csxFilePath);
            var assemblyPath = Path.Combine(artifactsDestination.FullName, $"{assemblyName}.dll");

            var configFile = await FileHelper.GetFileContentAsync(confFilePath);
            var config = JsonConvert.DeserializeObject<PackageConfig>(configFile);

            var metadata = await FileHelper.GetFileContentAsync(metadataFilePath);

            var workerId = string.Equals(config?.Type, "gist", StringComparison.OrdinalIgnoreCase) ? "GistWorker" : "DetectorWorker";
            await FileHelper.WriteToFileAsync(artifactsDestination.FullName, _workerIdFileName, workerId);

            if (workerId.Equals(this.Name, StringComparison.CurrentCultureIgnoreCase))
            {
                await this.CreateOrUpdateCacheAsync(artifactsDestination, lastModifiedMarker, scriptText, assemblyPath, metadata);
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
        public override async Task CreateOrUpdateCacheAsync(DirectoryInfo destDir, string sha, string scriptText, string assemblyPath, string metadata)
        {
            if (_loadOnlyPublicDetectors && !regexPublicDetectors.Match(scriptText).Success)
            {
                return;
            }

            LogMessage($"Loading assembly : {assemblyPath}");
            var asm = Assembly.LoadFrom(assemblyPath);

            var newInvoker = new EntityInvoker(new EntityMetadata(scriptText, GetEntityType(), metadata, sha));
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
    }
}
