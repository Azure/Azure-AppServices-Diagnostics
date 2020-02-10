using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher.Workers
{
    /// <summary>
    /// An instance of IGithubWorker to specifically capture kusto configuration data from a github repo.
    /// </summary>
    public class GithubKustoConfigurationWorker : IGithubWorker
    {
        public string Name { get { return "KustoConfigurationWorker"; } }

        public Task CreateOrUpdateCacheAsync(DirectoryInfo subDir)
        {
            throw new NotImplementedException();
        }

        public Task CreateOrUpdateCacheAsync(DirectoryInfo destDir, string lastModifiedMarker, string scriptText, string assemblyPath, string metadata)
        {
            throw new NotImplementedException();
        }

        public Task CreateOrUpdateCacheAsync(IEnumerable<GithubEntry> githubEntries, DirectoryInfo artifactsDestination, string lastModifiedMarker)
        {
            throw new NotImplementedException();
        }
    }
}
