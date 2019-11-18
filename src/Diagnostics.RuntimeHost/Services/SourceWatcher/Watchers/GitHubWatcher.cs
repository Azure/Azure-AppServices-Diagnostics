// <copyright file="GitHubWatcher.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Services.SourceWatcher.Workers;
using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.Scripts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    /// <summary>
    /// Github watcher.
    /// </summary>
    public class GitHubWatcher : SourceWatcherBase
    {
        private Task _firstTimeCompletionTask;
        private string _rootContentApiPath;

        public readonly IGithubClient _githubClient;
        private readonly string _workerIdFileName = "workerId.txt";
        private readonly string _lastModifiedMarkerName = "_lastModified.marker";
        private readonly string _deleteMarkerName = "_delete.marker";
        private readonly string _cacheIdFileName = "cacheId.txt";

        // Load from configuration.
        private string _destinationCsxPath;

        private int _pollingIntervalInSeconds;

        private bool _loadOnlyPublicDetectors;

        protected override Task FirstTimeCompletionTask => _firstTimeCompletionTask;

        protected override string SourceName => "GitHub";

        private IDictionary<string, IGithubWorker> GithubWorkers { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubWatcher" /> class.
        /// </summary>
        /// <param name="env">Hosting environment.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="invokerCache">Invoker cache.</param>
        /// <param name="gistCache">Gist cache.</param>
        /// <param name="githubClient">Github client.</param>
        public GitHubWatcher(IHostingEnvironment env, IConfiguration configuration, IInvokerCacheService invokerCache, IGistCacheService gistCache, IGithubClient githubClient)
            : base(env, configuration, invokerCache, gistCache, "GithubWatcher")
        {
            _githubClient = githubClient;

            LoadConfigurations();

            #region Initialize Github Worker

            // TODO: Register the github worker with destination path.
            var gistWorker = new GithubGistWorker(gistCache, _loadOnlyPublicDetectors);
            var detectorWorker = new GithubDetectorWorker(invokerCache, _loadOnlyPublicDetectors);

            GithubWorkers = new Dictionary<string, IGithubWorker>
            {
                { gistWorker.Name, gistWorker },
                { detectorWorker.Name, detectorWorker }
            };

            #endregion Initialize Github Worker

            Start();
        }

        /// <summary>
        /// Start github watcher.
        /// </summary>
        public override void Start()
        {
            _firstTimeCompletionTask = StartWatcherInternal(true);
            StartPollingForChanges();
        }

        /// <summary>
        /// Create or update a package.
        /// </summary>
        /// <param name="pkg">Detector package.</param>
        /// <returns>Task for creating or updateing detector.</returns>
        public override async Task CreateOrUpdatePackage(Package pkg)
        {
            if (pkg == null)
            {
                throw new ArgumentNullException(nameof(pkg));
            }

            await _githubClient.CreateOrUpdateFiles(pkg.GetCommitContents(), pkg.GetCommitMessage());
        }

        /// <summary>
        /// Start github watcher
        /// </summary>
        /// <returns>Task for starting watcher.</returns>
        private async Task StartWatcherInternal(bool startup)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                LogMessage("SourceWatcher : Start");
                var destDirInfo = new DirectoryInfo(_destinationCsxPath);
                var destLastModifiedMarker = await FileHelper.GetFileContentAsync(destDirInfo.FullName, _lastModifiedMarkerName);

                var response = await _githubClient.Get(_rootContentApiPath, etag: destLastModifiedMarker);
                LogMessage($"Http call to repository root path completed. Status Code : {response.StatusCode.ToString()}");

                if (response.StatusCode >= HttpStatusCode.NotFound)
                {
                    var errorContent = string.Empty;
                    try
                    {
                        errorContent = await response.Content.ReadAsStringAsync();
                    }
                    catch { }

                    LogException($"Unexpected response while checking for detector modifications. Response : {errorContent}", null);
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
                    var directories = destDirInfo.EnumerateDirectories();
                    List<Task> tasks = new List<Task>(directories.Count());
                    foreach (DirectoryInfo subDir in directories)
                    {
                        var workerId = await GetWorkerIdAsync(subDir);

                        if (GithubWorkers.ContainsKey(workerId))
                        {
                            if (startup)
                            {
                                tasks.Add(GithubWorkers[workerId].CreateOrUpdateCacheAsync(subDir));
                            }
                            else
                            {
                                await GithubWorkers[workerId].CreateOrUpdateCacheAsync(subDir);
                            }
                        }
                        else
                        {
                            LogWarning($"Cannot find github worker with id {workerId}. Directory: {subDir.FullName}.");
                        }
                    }
                    if (startup)
                    {
                        await Task.WhenAll(tasks);
                    }

                    return;
                }

                LogMessage("Syncing local directories with github changes");
                var githubRootContentETag = GetHeaderValue(response, HeaderConstants.EtagHeaderName).Replace("W/", string.Empty);
                var githubDirectories = await response.Content.ReadAsAsyncCustom<GithubEntry[]>();

                List<Task> downloadContentUpdateCacheAndModifiedMarkerTasks = new List<Task>();
                foreach (var gitHubDir in githubDirectories)
                {
                    if (!gitHubDir.Type.Equals("dir", StringComparison.OrdinalIgnoreCase)) continue;

                    var subDir = new DirectoryInfo(Path.Combine(destDirInfo.FullName, gitHubDir.Name.ToLower()));
                    if (!subDir.Exists)
                    {
                        LogMessage($"Folder : {subDir.Name} present in github but not on local disk. Creating it...");
                        subDir.Create();
                    }

                    FileHelper.DeleteFileIfExists(subDir.FullName, _deleteMarkerName);
                    var subDirModifiedMarker = await FileHelper.GetFileContentAsync(subDir.FullName, _lastModifiedMarkerName);

                    // ETag matches
                    if (subDirModifiedMarker == gitHubDir.Sha)
                    {
                        var workerId = await GetWorkerIdAsync(subDir);

                        if (GithubWorkers.ContainsKey(workerId))
                        {
                            await GithubWorkers[workerId].CreateOrUpdateCacheAsync(subDir);
                        }
                        else
                        {
                            LogWarning($"Cannot find github worker with id {workerId}. Directory: {subDir.FullName}.");
                        }

                        continue;
                    }

                    LogMessage($"Detected changes in Github Folder : {gitHubDir.Name.ToLower()}. Syncing it locally ...");

                    downloadContentUpdateCacheAndModifiedMarkerTasks.Add(DownloadContentAndUpdateCacheAndModifiedMarker(gitHubDir, subDir));
                }

                await Task.WhenAll(downloadContentUpdateCacheAndModifiedMarkerTasks);

                await SyncLocalDirForDeletedEntriesInGitHub(githubDirectories, destDirInfo);

                await FileHelper.WriteToFileAsync(destDirInfo.FullName, _lastModifiedMarkerName, githubRootContentETag);
            }
            catch (Exception ex)
            {
                LogException(ex.Message, ex);
            }
            finally
            {
                stopWatch.Stop();
                LogMessage($"SourceWatcher : End, Time Taken: {stopWatch.ElapsedMilliseconds}");
            }
        }

        private async Task DownloadContentAndUpdateCacheAndModifiedMarker(GithubEntry gitHubDir, DirectoryInfo subDir)
        {
            try
            {
                // Specifically catching exceptions for downloading and loading assembilies for every detector.
                await DownloadContentAndUpdateCache(gitHubDir, subDir, gitHubDir.Name);
                await FileHelper.WriteToFileAsync(subDir.FullName, _lastModifiedMarkerName, gitHubDir.Sha);
            }
            catch (Exception downloadEx)
            {
                LogException(downloadEx.Message, downloadEx);
            }
        }

        private async void StartPollingForChanges()
        {
            await _firstTimeCompletionTask;

            do
            {
                await Task.Delay(_pollingIntervalInSeconds * 1000);
                await StartWatcherInternal(false);
            } while (true);
        }

        private async Task DownloadContentAndUpdateCache(GithubEntry parentGithubEntry, DirectoryInfo destDir, string folderName)
        {
            var assemblyName = Guid.NewGuid().ToString();
            var csxFilePath = string.Empty;
            var confFilePath = string.Empty;
            var metadataFilePath = string.Empty;
            var lastCacheId = string.Empty;
            var cacheIdFilePath = Path.Combine(destDir.FullName, _cacheIdFileName);

            var response = await _githubClient.Get(parentGithubEntry.Url);
            if (!response.IsSuccessStatusCode)
            {
                LogException($"GET Failed. Url : {parentGithubEntry.Url}, StatusCode : {response.StatusCode.ToString()}", null);
                return;
            }

            var githubFiles = await response.Content.ReadAsAsyncCustom<GithubEntry[]>();

            foreach (GithubEntry githubFile in githubFiles)
            {
                var fileExtension = githubFile.Name.Split(new char[] { '.' }).LastOrDefault();
                if (string.IsNullOrWhiteSpace(githubFile.Download_url) || string.IsNullOrWhiteSpace(fileExtension))
                {
                    // Skip Downloading any directory (with empty download url) or any file without an extension.
                    continue;
                }

                var downloadFilePath = Path.Combine(destDir.FullName, githubFile.Name.ToLower());
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
                // Extensions used in search model files
                else if (githubFile.Name.Split(".").Last() == "model" || githubFile.Name.Split(".").Last() == "index" || githubFile.Name.Split(".").Last() == "dict" || githubFile.Name.Split(".").Last() == "npy")
                {
                    LogMessage($"Found a search model, skipping the folder {folderName}");
                    return;
                }
                else
                {
                    // Use Guids for Assembly and PDB Names to ensure uniqueness.
                    downloadFilePath = Path.Combine(destDir.FullName, $"{assemblyName}.{fileExtension.ToLower()}");
                }

                LogMessage($"Begin downloading File : {githubFile.Name.ToLower()} and saving it as : {downloadFilePath}");
                await _githubClient.DownloadFile(githubFile.Download_url, downloadFilePath);
            }

            var scriptText = await FileHelper.GetFileContentAsync(csxFilePath);
            var assemblyPath = Path.Combine(destDir.FullName, $"{assemblyName}.dll");

            var configFile = await FileHelper.GetFileContentAsync(confFilePath);
            var config = JsonConvert.DeserializeObject<PackageConfig>(configFile);

            var metadata = await FileHelper.GetFileContentAsync(metadataFilePath);

            var workerId = string.Equals(config?.Type, "gist", StringComparison.OrdinalIgnoreCase) ? "GistWorker" : "DetectorWorker";
            await FileHelper.WriteToFileAsync(destDir.FullName, _workerIdFileName, workerId);

            if (GithubWorkers.ContainsKey(workerId))
            {
                await GithubWorkers[workerId].CreateOrUpdateCacheAsync(destDir, parentGithubEntry.Sha, scriptText, assemblyPath, metadata);
            }
            else
            {
                LogWarning($"Cannot find github worker with id {workerId}. Directory: {destDir.FullName}.");
            }
        }

        private async Task SyncLocalDirForDeletedEntriesInGitHub(GithubEntry[] githubDirectories, DirectoryInfo destDirInfo)
        {
            if (!destDirInfo.Exists) return;

            LogMessage("Checking for deleted folders in github");
            foreach (DirectoryInfo subDir in destDirInfo.EnumerateDirectories())
            {
                var dirExistsInGithub = githubDirectories.Any(p => p.Name.Equals(subDir.Name, StringComparison.OrdinalIgnoreCase));
                if (!dirExistsInGithub)
                {
                    LogMessage($"Folder : {subDir.Name} not present in github. Marking for deletion");
                    // remove the entry from cache and mark the folder for deletion.s
                    var cacheId = await FileHelper.GetFileContentAsync(subDir.FullName, _cacheIdFileName);
                    if (!string.IsNullOrWhiteSpace(cacheId) && _invokerCache.TryRemoveValue(cacheId, out EntityInvoker invoker))
                    {
                        invoker.Dispose();
                    }

                    if (!string.IsNullOrWhiteSpace(cacheId) && _gistCache.TryRemoveValue(cacheId, out var gist))
                    {
                        // No action.
                    }

                    await FileHelper.WriteToFileAsync(subDir.FullName, _deleteMarkerName, "true");
                }
            }
        }

        private async Task<string> GetWorkerIdAsync(DirectoryInfo subDir)
        {
            var workerId = await FileHelper.GetFileContentAsync(subDir.FullName, _workerIdFileName);
            if (string.IsNullOrWhiteSpace(workerId))
            {
                return "DetectorWorker";
            }

            return workerId;
        }

        private static string GetHeaderValue(HttpResponseMessage responseMsg, string headerName)
        {
            if (responseMsg.Headers.TryGetValues(headerName, out IEnumerable<string> values))
            {
                return values.FirstOrDefault() ?? string.Empty;
            }

            return string.Empty;
        }

        private void LoadConfigurations()
        {
            _rootContentApiPath = $@"https://api.github.com/repos/{_githubClient.UserName}/{_githubClient.RepoName}/contents?ref={_githubClient.Branch}";
            var pollingIntervalvalue = string.Empty;
            
            _destinationCsxPath = (_config[$"SourceWatcher:Github:{RegistryConstants.DestinationScriptsPathKey}"]).ToString();
            pollingIntervalvalue = (_config[$"SourceWatcher:{RegistryConstants.PollingIntervalInSecondsKey}"]).ToString();

            if (!bool.TryParse((_config[$"SourceWatcher:{RegistryConstants.LoadOnlyPublicDetectorsKey}"]), out _loadOnlyPublicDetectors))
            {
                _loadOnlyPublicDetectors = false;
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
