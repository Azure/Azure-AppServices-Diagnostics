using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Utilities;
using Newtonsoft.Json;
using Octokit;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher.Workers
{
    /// <summary>
    /// An instance of IGithubWorker to specifically capture kusto configuration data from a github repo.
    /// </summary>
    public class GithubKustoConfigurationWorker : IGithubWorker
    {
        public string Name { get { return "KustoConfigurationWorker"; } }
        private IGithubClient _githubClient;
        private IKustoMappingsCacheService _cacheService;
        private const string _kustoClusterFileName = "kustoClusterMappings";

        public GithubKustoConfigurationWorker(IGithubClient githubClient, IKustoMappingsCacheService cacheService)
        {
            _githubClient = githubClient;
            _cacheService = cacheService;
        }

        public async Task CreateOrUpdateCacheAsync(DirectoryInfo subDir)
        {
            if (IsWorkerApplicable(subDir))
            {
                if (!_cacheService.ContainsKey(GetCacheId(subDir.Name, _kustoClusterFileName)))
                {
                    var kustoMappingsStringContent = await FileHelper.GetFileContentAsync(subDir.FullName, _kustoClusterFileName);
                    var kustoMappings = (Table)JsonConvert.DeserializeObject(kustoMappingsStringContent, typeof(Table));
                    _cacheService.AddOrUpdate(GetCacheId(subDir.Name, _kustoClusterFileName), kustoMappings);
                }
            }
        }

        public Task CreateOrUpdateCacheAsync(DirectoryInfo destDir, string lastModifiedMarker, string scriptText, string assemblyPath, string metadata)
        {
            //no op
            throw new NotImplementedException();
        }

        public async Task CreateOrUpdateCacheAsync(IEnumerable<GithubEntry> githubEntries, DirectoryInfo artifactsDestination, string lastModifiedMarker)
        {
            if (IsWorkerApplicable(githubEntries))
            {
                foreach (var githubFile in githubEntries)
                {
                    string downloadFilePath = Path.Combine(artifactsDestination.FullName, githubFile.Name.ToLower());
                    LogMessage($"Begin downloading File : {githubFile.Name.ToLower()} and saving it as : {downloadFilePath}");
                    await _githubClient.DownloadFile(githubFile.Download_url, downloadFilePath);

                    _cacheService.TryRemoveValue(githubFile.Url, out Table throwAway);

                    var kustoMappingsStringContent = await FileHelper.GetFileContentAsync(artifactsDestination.FullName, githubFile.Name);
                    var kustoMappings = (Table) JsonConvert.DeserializeObject(kustoMappingsStringContent, typeof(Table));

                    _cacheService.AddOrUpdate(GetCacheId(artifactsDestination.Name, githubFile), kustoMappings);
                }
            }
        }

        private bool IsWorkerApplicable(IEnumerable<GithubEntry> githubEntries)
        {
            var selectedGitHubEntries = githubEntries.Where(x => x.Name.Contains(_kustoClusterFileName, StringComparison.CurrentCultureIgnoreCase));
            return selectedGitHubEntries.Any();
        }

        private bool IsWorkerApplicable(DirectoryInfo dir)
        {
            return dir.EnumerateFiles().Any(x => x.Name.Contains(_kustoClusterFileName, StringComparison.CurrentCultureIgnoreCase)
            && x.Extension.Equals("json", StringComparison.CurrentCultureIgnoreCase));
        }

        private string GetCacheId(string folderName, GithubEntry githubFile)
        {
            return GetCacheId(folderName, githubFile.Name);
        }

        private string GetCacheId(string foldername, string fileName)
        {
            return foldername + fileName;
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
    }
}
