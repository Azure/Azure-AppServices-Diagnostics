using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    public class GitHubWatcher : SourceWatcherBase
    {
        private Task _firstTimeCompletionTask;
        private IGithubClient _githubClient;
        private string _rootContentApiPath;
        private readonly string _lastModifiedMarkerName;
        private readonly string _deleteMarkerName;
        private readonly string _cacheIdFileName;

        private string _destinationCsxPath;
        private int _pollingIntervalInSeconds;

        protected override Task FirstTimeCompletionTask => _firstTimeCompletionTask;

        public GitHubWatcher(IHostingEnvironment env, IConfiguration configuration, ICache<string, EntityInvoker> invokerCache, IGithubClient githubClient)
            : base(env, configuration, invokerCache, "GithubWatcher")
        {
            _githubClient = githubClient;

            _lastModifiedMarkerName = "_lastModified.marker";
            _deleteMarkerName = "_delete.marker";
            _cacheIdFileName = "cacheId.txt";
            LoadConfigurations();
            Start();
        }

        public override void Start()
        {
            _firstTimeCompletionTask = StartWatcherInternal();
            StartPollingForChanges();
        }

        private async Task StartWatcherInternal()
        {
            try
            {
                LogMessage("SourceWatcher : Start");
                DirectoryInfo destDirInfo = new DirectoryInfo(_destinationCsxPath);
                string destLastModifiedMarker = await FileHelper.GetFileContentAsync(destDirInfo.FullName, _lastModifiedMarkerName);
                HttpResponseMessage response = await _githubClient.Get(_rootContentApiPath, etag: destLastModifiedMarker);
                LogMessage($"Http call to repository root path completed. Status Code : {response.StatusCode.ToString()}");

                if (response.StatusCode >= HttpStatusCode.NotFound)
                {
                    string errorContent = string.Empty;
                    try
                    {
                        errorContent = await response.Content.ReadAsStringAsync();
                    }
                    catch (Exception) { }

                    LogException($"Unexpected resonse from repository root path. Response : {errorContent}", null);
                    return;
                }

                if (response.StatusCode == HttpStatusCode.NotModified)
                {
                    /*
                     * If No changes detected on Github Root Directory, skip download.
                     * Make Sure this entity is loaded in Invoker cache for runtime.
                     * This codepath will be mostly used when the process restarts or machine reboot (and no changes are done in scripts source).
                     */
                    LogMessage($"Checking if any invoker present locally needs to be added in cache");
                    foreach (DirectoryInfo subDir in destDirInfo.EnumerateDirectories())
                    {
                        await AddInvokerToCacheIfNeeded(subDir);
                    }

                    return;
                }

                LogMessage("Syncing local directories with github changes");
                string githubRootContentETag = GetHeaderValue(response, HeaderConstants.EtagHeaderName).Replace("W/", string.Empty);
                GithubEntry[] githubDirectories = await response.Content.ReadAsAsyncCustom<GithubEntry[]>();

                foreach (GithubEntry gitHubDir in githubDirectories)
                {
                    if (!gitHubDir.Type.Equals("dir", StringComparison.OrdinalIgnoreCase)) continue;

                    DirectoryInfo subDir = new DirectoryInfo(Path.Combine(destDirInfo.FullName, gitHubDir.Name));
                    if (!subDir.Exists)
                    {
                        LogMessage($"Folder : {subDir.Name} present in github but not on local disk. Creating it...");
                        subDir.Create();
                    }

                    FileHelper.DeleteFileIfExists(subDir.FullName, _deleteMarkerName);
                    string subDirModifiedMarker = await FileHelper.GetFileContentAsync(subDir.FullName, _lastModifiedMarkerName);

                    if (subDirModifiedMarker == gitHubDir.Sha)
                    {
                        await AddInvokerToCacheIfNeeded(subDir);
                        continue;
                    }

                    LogMessage($"Deteced changes in Github Folder : {gitHubDir.Name}. Syncing it locally ...");

                    await DownloadContentAndUpdateInvokerCache(gitHubDir, subDir);
                    await FileHelper.WriteToFileAsync(subDir.FullName, _lastModifiedMarkerName, gitHubDir.Sha);
                }

                await SyncLocalDirForDeletedEntriesInGitHub(githubDirectories, destDirInfo);

                await FileHelper.WriteToFileAsync(destDirInfo.FullName, _lastModifiedMarkerName, githubRootContentETag);
            }
            catch (Exception ex)
            {
                LogException(ex.Message, ex);
            }
            finally
            {
                LogMessage("SourceWatcher : End");
            }
        }

        private async void StartPollingForChanges()
        {
            await _firstTimeCompletionTask;

            do
            {
                await Task.Delay(_pollingIntervalInSeconds * 1000);
                await StartWatcherInternal();

            } while (true);
        }

        private async Task AddInvokerToCacheIfNeeded(DirectoryInfo subDir)
        {
            string cacheId = await FileHelper.GetFileContentAsync(subDir.FullName, _cacheIdFileName);
            if (string.IsNullOrWhiteSpace(cacheId) || !_invokerCache.TryGetValue(cacheId, out EntityInvoker invoker))
            {
                LogMessage($"Folder : {subDir.FullName} missing in invoker cache.");
                FileInfo mostRecentAssembly = GetMostRecentFileByExtension(subDir, ".dll");
                FileInfo csxScriptFile = GetMostRecentFileByExtension(subDir, ".csx");
                FileInfo deleteMarkerFile = new FileInfo(Path.Combine(subDir.FullName, _deleteMarkerName));

                if (mostRecentAssembly == default(FileInfo) || csxScriptFile == default(FileInfo))
                {
                    LogWarning($"No Assembly file (.dll) or Csx File found (.csx). Skipping cache update");
                    return;
                }

                if (deleteMarkerFile.Exists)
                {
                    LogMessage("Folder marked for deletion. Skipping cache update");
                    return;
                }

                string scriptText = await FileHelper.GetFileContentAsync(csxScriptFile.FullName);
                LogMessage($"Loading assembly : {mostRecentAssembly.FullName}");
                Assembly asm = Assembly.LoadFrom(mostRecentAssembly.FullName);
                invoker = new EntityInvoker(new EntityMetadata(scriptText));
                invoker.InitializeEntryPoint(asm);

                if (invoker.EntryPointDefinitionAttribute != null)
                {
                    LogMessage($"Updating cache with  new invoker with id : {invoker.EntryPointDefinitionAttribute.Id}");
                    _invokerCache.AddOrUpdate(invoker.EntryPointDefinitionAttribute.Id, invoker);
                    await FileHelper.WriteToFileAsync(subDir.FullName, _cacheIdFileName, invoker.EntryPointDefinitionAttribute.Id);
                }
                else
                {
                    LogWarning("Missing Entry Point Definition attribute. skipping cache update");
                }
            }
        }

        private async Task DownloadContentAndUpdateInvokerCache(GithubEntry parentGithubEntry, DirectoryInfo destDir)
        {
            string assemblyName = Guid.NewGuid().ToString();
            string csxFilePath = string.Empty;
            string lastCacheId = string.Empty;
            string cacheIdFilePath = Path.Combine(destDir.FullName, _cacheIdFileName);
            Assembly asm;

            HttpResponseMessage response = await _githubClient.Get(parentGithubEntry.Url);
            if (!response.IsSuccessStatusCode)
            {
                LogException($"GET Failed. Url : {parentGithubEntry.Url}, StatusCode : {response.StatusCode.ToString()}", null);
                return;
            }

            GithubEntry[] githubFiles = await response.Content.ReadAsAsyncCustom<GithubEntry[]>();

            foreach (GithubEntry githubFile in githubFiles)
            {
                string fileExtension = githubFile.Name.Split(new char[] { '.' }).LastOrDefault();
                if (string.IsNullOrWhiteSpace(githubFile.Download_url) || string.IsNullOrWhiteSpace(fileExtension))
                {
                    // Skip Downloading any directory (with empty download url) or any file without an extension.
                    continue;
                }

                string downloadFilePath = Path.Combine(destDir.FullName, githubFile.Name);
                if (fileExtension.ToLower().Equals("csx"))
                {
                    csxFilePath = downloadFilePath;
                }
                else
                {
                    // Use Guids for Assembly and PDB Names to ensure uniqueness.
                    downloadFilePath = Path.Combine(destDir.FullName, $"{assemblyName}.{fileExtension.ToLower()}");
                }

                LogMessage($"Begin downloading File : {githubFile.Name} and saving it as : {downloadFilePath}");
                await _githubClient.DownloadFile(githubFile.Download_url, downloadFilePath);
            }

            string scriptText = await FileHelper.GetFileContentAsync(csxFilePath);

            string assemblyPath = Path.Combine(destDir.FullName, $"{assemblyName}.dll");
            LogMessage($"Loading assembly : {assemblyPath}");
            asm = Assembly.LoadFrom(assemblyPath);

            EntityInvoker newInvoker = new EntityInvoker(new EntityMetadata(scriptText));
            newInvoker.InitializeEntryPoint(asm);

            // Remove the Old Invoker from Cache
            lastCacheId = await FileHelper.GetFileContentAsync(cacheIdFilePath);
            if (!string.IsNullOrWhiteSpace(lastCacheId) && _invokerCache.TryRemoveValue(lastCacheId, out EntityInvoker oldInvoker))
            {
                LogMessage($"Removing old invoker with id : {oldInvoker.EntryPointDefinitionAttribute.Id} from Cache");
                oldInvoker.Dispose();
            }

            // Add new invoker to Cache and update Cache Id File
            if (newInvoker.EntryPointDefinitionAttribute != null)
            {
                LogMessage($"Updating cache with  new invoker with id : {newInvoker.EntryPointDefinitionAttribute.Id}");
                _invokerCache.AddOrUpdate(newInvoker.EntryPointDefinitionAttribute.Id, newInvoker);
                await FileHelper.WriteToFileAsync(cacheIdFilePath, newInvoker.EntryPointDefinitionAttribute.Id);
            }
            else
            {
                LogWarning("Missing Entry Point Definition attribute. skipping cache update");
            }
        }

        private async Task SyncLocalDirForDeletedEntriesInGitHub(GithubEntry[] githubDirectories, DirectoryInfo destDirInfo)
        {
            if (!destDirInfo.Exists) return;

            LogMessage("Checking for deleted folders in github");
            foreach (DirectoryInfo subDir in destDirInfo.EnumerateDirectories())
            {
                bool dirExistsInGithub = githubDirectories.Any(p => p.Name.Equals(subDir.Name));
                if (!dirExistsInGithub)
                {
                    LogMessage($"Folder : {subDir.Name} not present in github. Marking for deletion");
                    // remove the entry from cache and mark the folder for deletion.s
                    string cacheId = await FileHelper.GetFileContentAsync(subDir.FullName, _cacheIdFileName);
                    if (!string.IsNullOrWhiteSpace(cacheId) && _invokerCache.TryRemoveValue(cacheId, out EntityInvoker invoker))
                    {
                        invoker.Dispose();
                    }

                    await FileHelper.WriteToFileAsync(subDir.FullName, _deleteMarkerName, "true");
                }
            }
        }

        private string GetHeaderValue(HttpResponseMessage responseMsg, string headerName)
        {
            if (responseMsg.Headers.TryGetValues(headerName, out IEnumerable<string> values))
            {
                return values.FirstOrDefault() ?? string.Empty;
            }

            return string.Empty;
        }

        private FileInfo GetMostRecentFileByExtension(DirectoryInfo dir, string extension)
        {
            return dir.GetFiles().Where(p => (!string.IsNullOrWhiteSpace(p.Extension) && p.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
        }

        private void LoadConfigurations()
        {
            _rootContentApiPath = $@"https://api.github.com/repos/{_githubClient.UserName}/{_githubClient.RepoName}/contents?ref={_githubClient.Branch}";
            string pollingIntervalvalue = string.Empty;
            if (_env.IsProduction())
            {
                _destinationCsxPath = (string)Registry.GetValue(RegistryConstants.GithubWatcherRegistryPath, RegistryConstants.DestinationScriptsPathKey, string.Empty);
                pollingIntervalvalue = (string)Registry.GetValue(RegistryConstants.SourceWatcherRegistryPath, RegistryConstants.PollingIntervalInSecondsKey, string.Empty);
            }
            else
            {
                _destinationCsxPath = (_config[$"SourceWatcher:Github:{RegistryConstants.DestinationScriptsPathKey}"]).ToString();
                pollingIntervalvalue = (_config[$"SourceWatcher:{RegistryConstants.PollingIntervalInSecondsKey}"]).ToString();
            }

            if (!int.TryParse(pollingIntervalvalue, out _pollingIntervalInSeconds))
            {
                _pollingIntervalInSeconds = HostConstants.WatcherDefaultPollingIntervalInSeconds;
            }

            if (!Directory.Exists(_destinationCsxPath))
            {
                Directory.CreateDirectory(_destinationCsxPath);
            }
        }
    }
}
