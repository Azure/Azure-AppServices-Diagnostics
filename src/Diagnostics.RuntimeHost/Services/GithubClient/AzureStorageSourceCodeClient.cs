using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services.StorageService;
using Diagnostics.RuntimeHost.Utilities;
using Octokit;

namespace Diagnostics.RuntimeHost.Services
{
    /// <summary>
    /// A temporary azure storage implementation of the IGitHubClient library.
    /// </summary>
    public sealed class AzureStorageSourceCodeClient : IGithubClient
    {
        private IStorageService _storageService;

        public string UserName => string.Empty;

        public string RepoName => string.Empty;

        public string Branch => string.Empty;

        public AzureStorageSourceCodeClient(IStorageService storageService)
        {
            _storageService = storageService;
        }

        public Task CreateOrUpdateFile(string destinationFilePath, string content, string commitMessage, bool convertContentToBase64 = true)
        {
            throw new NotImplementedException();
        }

        public async Task CreateOrUpdateFiles(IEnumerable<CommitContent> commits, string commitMessage)
        {
            if (commits.Any(content => content.FilePath.Contains("kustoClusterMappings", StringComparison.CurrentCultureIgnoreCase)))
            {
                //no op
                return;
            }

            foreach (var commit in commits)
            {
                if (!Equals(commit.EncodingType, EncodingType.Base64))
                {
                    commit.Content = InternalAPIHelper.Base64Encode(commit.Content);
                }

                //filter out dll and pdb files since this is handled already by storage watcher
                if (commit.FilePath.EndsWith(".dll") || commit.FilePath.EndsWith(".pdb"))
                {
                    continue;
                }

                await _storageService.LoadBlobToContainer(commit.FilePath, commit.Content).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task DownloadFile(string fileUrl, string destinationPath)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> Get(string url, string etag = "")
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> Get(HttpRequestMessage request)
        {
            throw new NotImplementedException();
        }

        public string GetContentUrl(string path)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetFileContent(string filepath)
        {
            var contentBytes = await _storageService.GetBlobByName(filepath);
            var contentString = Encoding.UTF8.GetString(contentBytes);
            return contentString;
        }

        public Task<string> GetLatestSha()
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetTreeBySha(string sha, string etag = "")
        {
            throw new NotImplementedException();
        }

        Task<GitHubCommit> IGithubClient.GetCommitByPath(string filePath)
        {
            return Task.FromResult<GitHubCommit>(null);
        }
    }
}
