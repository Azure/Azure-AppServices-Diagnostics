using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Utilities;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher.Workers
{
    /// <summary>
    /// Gist worker.
    /// </summary>
    public class GithubGistWorker : GithubWorkerBase
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        public override string Name => "GistWorker";

        /// <summary>
        /// Gets the gist cache.
        /// </summary>
        public IGistCacheService GistCache { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GithubGistWorker"/> class.
        /// </summary>
        /// <param name="gistCache">Gist cache service.</param>
        public GithubGistWorker(IGistCacheService gistCache)
        {
            GistCache = gistCache;
        }

        /// <summary>
        /// Create or update cache.
        /// </summary>
        /// <param name="subDir">Directory info</param>
        /// <returns>Task for adding item to cache.</returns>
        public override async Task CreateOrUpdateCacheAsync(DirectoryInfo subDir)
        {
            try
            {
                // Get cached id from local file.
                var cacheId = await FileHelper.GetFileContentAsync(subDir.FullName, _cacheIdFileName);

                if (string.IsNullOrWhiteSpace(cacheId) || !GistCache.TryGetValue(cacheId, out GistEntry gist))
                {
                    LogMessage($"Folder : {subDir.FullName} missing in gist cache.");

                    // Check if delete marker file exists.
                    var deleteMarkerFile = new FileInfo(Path.Combine(subDir.FullName, _deleteMarkerName));
                    if (deleteMarkerFile.Exists)
                    {
                        LogMessage("Folder marked for deletion. Skipping cache update");
                        return;
                    }

                    // Download csx file.
                    var csxScriptFile = GetMostRecentFileByExtension(subDir, ".csx");
                    if (csxScriptFile == default(FileInfo))
                    {
                        LogWarning($"No Csx File found (.csx). Skipping cache update");
                        return;
                    }
                    var scriptText = await FileHelper.GetFileContentAsync(csxScriptFile.FullName);

                    // Get package json and deserialize to gist cache entry.
                    var jsonFile = GetMostRecentFileByExtension(subDir, ".json");
                    var config = await FileHelper.GetFileContentAsync(jsonFile.FullName);
                    gist = JsonConvert.DeserializeObject<GistEntry>(config);
                    gist.CodeString = scriptText;

                    LogMessage($"Updating cache with new gist with id : {gist.Id}");
                    GistCache.AddOrUpdate(gist.Id, gist);

                    // Write back cache id.
                    await FileHelper.WriteToFileAsync(subDir.FullName, _cacheIdFileName, gist.Id);
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
            // Read json file and deserialize to gist entry.
            var cacheIdFilePath = Path.Combine(destDir.FullName, _cacheIdFileName);
            var jsonFilePath = Path.Combine(destDir.FullName, "package.json");
            var config = await FileHelper.GetFileContentAsync(jsonFilePath);
            var gistEntry = JsonConvert.DeserializeObject<GistEntry>(config);
            gistEntry.CodeString = scriptText;

            var lastCacheId = await FileHelper.GetFileContentAsync(cacheIdFilePath);
            if (!string.IsNullOrWhiteSpace(lastCacheId) && GistCache.TryRemoveValue(lastCacheId, out GistEntry tmp))
            {
                LogMessage($"Removing old gist with id: {lastCacheId} from Cache");
            }

            LogMessage($"Updating cache with new gist with id: {gistEntry.Id}");
            GistCache.AddOrUpdate(gistEntry.Id, gistEntry);
            await FileHelper.WriteToFileAsync(cacheIdFilePath, gistEntry.Id);
        }
    }
}
