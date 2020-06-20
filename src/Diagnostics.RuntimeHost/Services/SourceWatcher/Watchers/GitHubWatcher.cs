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
using System.Diagnostics;
using System.Threading;
using Diagnostics.DataProviders;
using System.Security.Policy;
using Newtonsoft.Json.Linq;
using Kusto.Cloud.Platform.Utils;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    /// <summary>
    /// Github watcher.
    /// </summary>
    public class GitHubWatcher : SourceWatcherBase
    {
        private Task _firstTimeCompletionTask;
        private string _rootContentApiPath;
        private Task cleanDeletedFileTask;

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
        /// Latest Sha received from GitHub API
        /// </summary>
        private string LatestSha { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubWatcher" /> class.
        /// </summary>
        /// <param name="env">Hosting environment.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="invokerCache">Invoker cache.</param>
        /// <param name="gistCache">Gist cache.</param>
        /// <param name="githubClient">Github client.</param>
        public GitHubWatcher(IHostingEnvironment env, IConfiguration configuration, IInvokerCacheService invokerCache, IGistCacheService gistCache, IKustoMappingsCacheService kustoMappingsCache, IGithubClient githubClient)
            : base(env, configuration, invokerCache, gistCache, kustoMappingsCache,  "GithubWatcher")
        {
            _githubClient = githubClient;

            LoadConfigurations();

            #region Initialize Github Worker

            // TODO: Register the github worker with destination path.
            var gistWorker = new GithubGistWorker(gistCache, _loadOnlyPublicDetectors);
            var detectorWorker = new GithubDetectorWorker(invokerCache, _loadOnlyPublicDetectors);
            var kustoMappingsWorker = new GithubKustoConfigurationWorker(kustoMappingsCache);

            GithubWorkers = new Dictionary<string, IGithubWorker>
            {
                { gistWorker.Name, gistWorker },
                { detectorWorker.Name, detectorWorker },
                { kustoMappingsWorker.Name, kustoMappingsWorker }
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
            cleanDeletedFileTask = CleanupFilesForDeletion();
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

        public override async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpResponseMessage response = null;
            Exception healthCheckException = null;
            HealthCheckResult result;

            try
            {
                response = await _githubClient.Get(_rootContentApiPath);
            }
            catch(Exception ex)
            {
                healthCheckException = ex;
            }
            finally
            {
                result = new HealthCheckResult(healthCheckException == null ? response != null && response.IsSuccessStatusCode ? HealthStatus.Healthy : HealthStatus.Unhealthy : HealthStatus.Unhealthy,
                    "GitHubWatcherService", null, healthCheckException, new Dictionary<string, object>
                    {
                        { "HTTP Status Code", response != null ? response.StatusCode : 0 }
                    });
            }

            return result;
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
                LogMessage($"SourceWatcher : Start, startup = {startup.ToString()}");
                var destDirInfo = new DirectoryInfo(_destinationCsxPath);
                var destLastModifiedMarker = await FileHelper.GetFileContentAsync(destDirInfo.FullName, _lastModifiedMarkerName);
                
                if(string.IsNullOrWhiteSpace(LatestSha))
                {
                    LatestSha = await _githubClient.GetLatestSha();
                }

                var response = await _githubClient.GetTreeBySha(sha: LatestSha, etag: destLastModifiedMarker);

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
                    LogMessage($"Github directories present locally in {destDirInfo.FullName} : {directories.Count()}");
                    List<Task> tasks = new List<Task>(directories.Count());
                    
                    foreach (DirectoryInfo subDir in directories)
                    {
                        foreach (var githubWorker in GithubWorkers.Values)
                        {
                            if (startup)
                            {
                                tasks.Add(githubWorker.CreateOrUpdateCacheAsync(subDir));
                            }
                            else
                            {
                                await githubWorker.CreateOrUpdateCacheAsync(subDir);
                            }
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
                var rawGithubResponse = await response.Content.ReadAsStringAsync();
                var githubTrees = JObject.Parse(rawGithubResponse);
                var githubDirectories = githubTrees["tree"].ToObject<GithubEntry[]>();
                var githubLatestSha = (string)githubTrees["sha"];
                githubDirectories.ForEach(githubDir =>
                {
                    githubDir.Name = githubDir.Name ?? githubDir.Path;
                });

                LogMessage($"Total number of directors returned by Github: {githubDirectories.Length}");
                List<Task> downloadContentUpdateCacheAndModifiedMarkerTasks = new List<Task>();
                foreach (var gitHubDir in githubDirectories)
                {
                   // if (!gitHubDir.Type.Equals("dir", StringComparison.OrdinalIgnoreCase)) continue;

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
                        foreach (var githubWorker in GithubWorkers.Values)
                        {
                            await githubWorker.CreateOrUpdateCacheAsync(subDir);
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
            var searchModelFiles = new string[] { "model", "index", "dict", "npy" };

            if(parentGithubEntry.Url.Contains("trees"))
            {
                parentGithubEntry.Url = _githubClient.GetContentUrl(parentGithubEntry.Name);
            }
            var response = await _githubClient.Get(parentGithubEntry.Url);
            if (!response.IsSuccessStatusCode)
            {
                LogException($"GET Failed. Url : {parentGithubEntry.Url}, StatusCode : {response.StatusCode.ToString()}", null);
                return;
            }

            var githubFiles = await response.Content.ReadAsAsyncCustom<GithubEntry[]>();

            // Skip extensions used in search model files
            if (githubFiles.Any(x => searchModelFiles.Any(searchModelExtension => !string.IsNullOrWhiteSpace(x.Name) 
            && x.Name.EndsWith("." + searchModelExtension, StringComparison.CurrentCultureIgnoreCase))))
            {
                return;
            }

            var selectedGithubFiles = githubFiles
                // Skip Downloading any directory (with empty download url) or any file without an extension.
                .Where(x => !string.IsNullOrWhiteSpace(x.Download_url) 
                && !string.IsNullOrWhiteSpace(x.Name.Split(new char[] { '.' }).LastOrDefault()));

            var assemblyName = Guid.NewGuid().ToString();
            var expectedFiles = new string[] { "csx", "package.json", "metadata.json" , "kustoClusterMappings"};

            // Identify files to be downloaded
            if (!selectedGithubFiles.Any(x => expectedFiles.Any(y => x.Name.Contains(y, StringComparison.CurrentCultureIgnoreCase))))
            {
                return;
            }

            foreach (GithubEntry githubFile in selectedGithubFiles)
            {
                var fileExtension = githubFile.Name.Split(new char[] { '.' }).LastOrDefault();

                var downloadFilePath = Path.Combine(destDir.FullName, githubFile.Name.ToLower());

                // Use Guids for Assembly and PDB Names to ensure uniqueness.
                if (fileExtension.Equals("dll") || fileExtension.Equals("pdb"))
                {
                    downloadFilePath = Path.Combine(destDir.FullName, $"{assemblyName}.{fileExtension.ToLower()}");
                }

                // Remove token from download Url as this is now deperecated.
                // https://developer.github.com/changes/2020-02-10-deprecating-auth-through-query-param/
                // We already add the token in the header so the token in the Uri is redundant anyway.
                var downloadUrl = new UriBuilder(githubFile.Download_url);
                var paramValues = System.Web.HttpUtility.ParseQueryString(downloadUrl.Query);
                if (paramValues.AllKeys.Contains("token"))
                {
                    paramValues.Remove("token");
                }
                downloadUrl.Query = paramValues.ToString();

                LogMessage($"Begin downloading File : {githubFile.Name.ToLower()} and saving it as : {downloadFilePath}");
                await _githubClient.DownloadFile(downloadUrl.Uri.ToString(), downloadFilePath);
            }

            //At this point, required file have been downloaded by GitHubWatcher to destination folder. 
            // Source watcher should just read from the destination folder and update their respective cache.

            foreach (IGithubWorker githubWorker in GithubWorkers.Values)
            {
                try
                {
                    await githubWorker.CreateOrUpdateCacheAsync(selectedGithubFiles, destDir, parentGithubEntry.Sha);
                }
                catch (Exception ex)
                {
                    LogException($"Failed to execute github worker with id {githubWorker.Name}. Directory: {destDir.FullName}", ex);
                }
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

                    _kustoMappingsCache.TryRemoveValue(subDir.Name, out var throwAway);

                    await FileHelper.WriteToFileAsync(subDir.FullName, _deleteMarkerName, "true");
                }
            }
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
     
        private async Task CleanupFilesForDeletion()
        {

            Stopwatch stopwatch = new Stopwatch();
            try
            {
                DirectoryInfo scriptsDir = new DirectoryInfo(_destinationCsxPath);

                if (!scriptsDir.Exists) return;
                stopwatch.Start();
                LogMessage($"Starting clean up method for directory {_destinationCsxPath}");
                foreach (DirectoryInfo subDir in scriptsDir.GetDirectories())
                {
                    if (File.Exists(Path.Combine(subDir.FullName, _deleteMarkerName)))
                    {
                        LogMessage($"Delete Marker Found. Deleting Directory : {subDir.FullName}");
                        FileHelper.DeleteFolderRecursive(subDir);
                    }
                    else
                    {
                        var assemblyFiles = subDir.GetFiles("*.dll").OrderByDescending(f => f.LastWriteTimeUtc).Skip(1).ToList();
                        var pdbFiles = subDir.GetFiles("*.pdb").OrderByDescending(f => f.LastWriteTimeUtc).Skip(1).ToList();

                        if(assemblyFiles != null && pdbFiles != null)
                        {
                            assemblyFiles.Concat(pdbFiles).ToList().ForEach((item) =>
                            {
                                LogMessage($"Deleting File : {item.FullName}");
                                item.IsReadOnly = false;
                                FileHelper.DeleteFileAsync(item.FullName);
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogException($"CleanupDataFilesMarkedForDeletion Failed. Exception : {ex.ToString()}", ex);
            }
            finally
            {
                stopwatch.Stop();
                LogMessage($"Clean up completed, time taken {stopwatch.ElapsedMilliseconds}");
            }
        }

    }
}
