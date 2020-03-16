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
    public class GithubKustoConfigurationWorker : GithubWorkerBase
    {
        public override string Name { get { return "KustoConfigurationWorker"; } }
        private IKustoMappingsCacheService _cacheService;
        private const string _kustoClusterFileName = "kustoClusterMappings";

        public GithubKustoConfigurationWorker(IKustoMappingsCacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public override async Task CreateOrUpdateCacheAsync(DirectoryInfo subDir)
        {
            var workerId = await FileHelper.GetFileContentAsync(subDir.FullName, _workerIdFileName);

            if (IsWorkerApplicable(subDir) || workerId.Equals(Name, StringComparison.CurrentCultureIgnoreCase))
            {
                // Check if delete marker file exists.
                var deleteMarkerFile = new FileInfo(Path.Combine(subDir.FullName, _deleteMarkerName));
                if (deleteMarkerFile.Exists)
                {
                    _cacheService.TryRemoveValue(subDir.Name, out var throwAway);
                    return;
                }

                var kustoMappingsStringContent = await FileHelper.GetFileContentAsync(subDir.FullName, $"{_kustoClusterFileName}.json");
                var kustoMappings = (List<Dictionary<string, string>>)JsonConvert.DeserializeObject(kustoMappingsStringContent, typeof(List<Dictionary<string, string>>));

                if (!_cacheService.ContainsKey(subDir.Name) || (_cacheService.TryGetValue(subDir.Name, out List<Dictionary<string, string>> value) && !value.Equals(kustoMappings)))
                {
                    _cacheService.AddOrUpdate(subDir.Name, kustoMappings);
                }
            }
        }

        public override Task CreateOrUpdateCacheAsync(DirectoryInfo destDir, string lastModifiedMarker, string scriptText, string assemblyPath, string metadata)
        {
            //no op
            throw new NotImplementedException();
        }

        public override async Task CreateOrUpdateCacheAsync(IEnumerable<GithubEntry> githubEntries, DirectoryInfo artifactsDestination, string lastModifiedMarker)
        {
            if (IsWorkerApplicable(githubEntries))
            {
                foreach (var githubFile in githubEntries)
                {                  
                    _cacheService.TryRemoveValue(artifactsDestination.Name, out List<Dictionary<string, string>> throwAway);

                    var kustoMappingsStringContent = await FileHelper.GetFileContentAsync(artifactsDestination.FullName, githubFile.Name);
                    var kustoMappings = (List<Dictionary<string, string>>) JsonConvert.DeserializeObject(kustoMappingsStringContent, typeof(List<Dictionary<string, string>>));

                    _cacheService.AddOrUpdate(artifactsDestination.Name, kustoMappings);
                }

                await FileHelper.WriteToFileAsync(artifactsDestination.FullName, _workerIdFileName, Name);
            }
        }

        private bool IsWorkerApplicable(IEnumerable<GithubEntry> githubEntries)
        {
            var selectedGitHubEntries = githubEntries.Where(x => x.Name.Contains(_kustoClusterFileName, StringComparison.CurrentCultureIgnoreCase));
            return selectedGitHubEntries.Any();
        }

        private bool IsWorkerApplicable(DirectoryInfo dir)
        {
            return dir.EnumerateFiles().Any(x => x.Name.EndsWith($"{_kustoClusterFileName}.json", StringComparison.CurrentCultureIgnoreCase));
        }
    }
}
