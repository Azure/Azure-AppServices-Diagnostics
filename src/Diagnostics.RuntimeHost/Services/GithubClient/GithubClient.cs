using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Octokit;
using static Diagnostics.Logger.HeaderConstants;

namespace Diagnostics.RuntimeHost.Services
{
    public interface IGithubClient : IDisposable
    {
        string UserName { get; }

        string RepoName { get; }

        string Branch { get; }

        Task<HttpResponseMessage> Get(string url, string etag = "");

        Task<HttpResponseMessage> Get(HttpRequestMessage request);

        Task DownloadFile(string fileUrl, string destinationPath);

        Task CreateOrUpdateFile(string destinationFilePath, string content, string commitMessage, bool convertContentToBase64 = true);

        Task CreateOrUpdateFiles(IEnumerable<CommitContent> commits, string commitMessage);
    }

    public class GithubClient : IGithubClient
    {
        private IHostingEnvironment _env;
        private IConfiguration _config;
        private string _userName;
        private string _repoName;
        private string _branch;
        private string _accessToken;
        private HttpClient _httpClient;
        private Octokit.GitHubClient _octokitClient;

        public string UserName => _userName;

        public string RepoName => _repoName;

        public string Branch => _branch;

        public GithubClient(IHostingEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _config = configuration;
            LoadConfigurations();
            ValidateConfigurations();
            InitializeHttpClient();
            InitializeOctokitClient();
        }

        public GithubClient(string userName, string repoName, string branch = "master", string accessToken = "")
        {
            _userName = userName;
            _repoName = repoName;
            _branch = !string.IsNullOrWhiteSpace(branch) ? branch : "master";
            _accessToken = accessToken ?? string.Empty;
            ValidateConfigurations();
            InitializeHttpClient();
            InitializeOctokitClient();
        }

        public Task<HttpResponseMessage> Get(string url, string etag = "")
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, AppendQueryStringParams(url));
            if (!string.IsNullOrWhiteSpace(etag))
            {
                request.Headers.Add(IfNoneMatchHeaderName, etag);
            }

            return Get(request);
        }

        public Task<HttpResponseMessage> Get(HttpRequestMessage request)
        {
            return _httpClient.SendAsync(request);
        }

        public async Task DownloadFile(string fileUrl, string destinationPath)
        {
            using (HttpResponseMessage httpResponse = await Get(fileUrl))
            {
                if (!httpResponse.IsSuccessStatusCode)
                {
                    var uri = new Uri(fileUrl);
                    throw new HttpRequestException($"Failed while getting resource : {uri.Scheme}:/{uri.AbsolutePath} . Http Status Code : {httpResponse.StatusCode}");
                }

                using (Stream srcStream = await httpResponse.Content.ReadAsStreamAsync(),
                        destStream = new FileStream(destinationPath, System.IO.FileMode.Create))
                {
                    await srcStream.CopyToAsync(destStream);
                }
            }
        }

        public async Task CreateOrUpdateFile(string filePath, string content, string commitMessage, bool convertContentToBase64 = true)
        {
            try
            {
                // try to get the file (and with the file the last commit sha)
                var existingFile = await _octokitClient.Repository.Content.GetAllContentsByRef(_userName, _repoName, filePath, _branch);

                // update the existing file
                var updateChangeSet = await _octokitClient.Repository.Content.UpdateFile(_userName, _repoName, filePath,
                   new UpdateFileRequest(commitMessage, content, existingFile.First().Sha, _branch, convertContentToBase64));
            }
            catch (NotFoundException)
            {
                var createFileRequest = new CreateFileRequest(commitMessage, content, _branch, convertContentToBase64);
                // if file is not found, create it
                var createChangeSet = await _octokitClient.Repository.Content.CreateFile(_userName, _repoName, filePath,
                    createFileRequest);
            }
        }

        public async Task CreateOrUpdateFiles(IEnumerable<CommitContent> commits, string commitMessage)
        {
            var headRef = $"heads/{_branch}";

            // Get current head ref.
            var reference = await _octokitClient.Git.Reference.Get(_userName, _repoName, headRef);
            var latestCommit = await _octokitClient.Git.Commit.Get(_userName, _repoName, reference.Object.Sha);

            // Build new tree.
            var tree = new NewTree
            {
                BaseTree = latestCommit.Tree.Sha
            };

            var tasks = commits.Select(c =>
            {
                return _octokitClient.Git.Blob.Create(
                    _userName,
                    _repoName,
                    new NewBlob
                    {
                        Content = c.Content,
                        Encoding = c.EncodingType
                    }).ContinueWith(
                    blob =>
                    {
                        return new NewTreeItem
                        {
                            Path = c.FilePath,
                            Mode = "100644",
                            Type = TreeType.Blob,
                            Sha = blob.Result.Sha
                        };
                    });
            });

            var treeItem = await Task.WhenAll(tasks);
            foreach (var item in treeItem)
            {
                tree.Tree.Add(item);
            }

            var response = await _octokitClient.Git.Tree.Create(_userName, _repoName, tree);

            var newCommit = new NewCommit(commitMessage, response.Sha, reference.Object.Sha);
            var commit = await _octokitClient.Git.Commit.Create(_userName, _repoName, newCommit);

            // Update current head ref.
            await _octokitClient.Git.Reference.Update(_userName, _repoName, headRef, new ReferenceUpdate(commit.Sha));
        }

        public void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }
        }

        private void LoadConfigurations()
        {
            if (_env.IsProduction())
            {
                _userName = (string)Registry.GetValue(RegistryConstants.GithubWatcherRegistryPath, RegistryConstants.GithubUserNameKey, string.Empty);
                _repoName = (string)Registry.GetValue(RegistryConstants.GithubWatcherRegistryPath, RegistryConstants.GithubRepoNameKey, string.Empty);
                _branch = (string)Registry.GetValue(RegistryConstants.GithubWatcherRegistryPath, RegistryConstants.GithubBranchKey, string.Empty);
                _accessToken = (string)Registry.GetValue(RegistryConstants.GithubWatcherRegistryPath, RegistryConstants.GithubAccessTokenKey, string.Empty);
            }
            else
            {
                _userName = _config[$"SourceWatcher:Github:{RegistryConstants.GithubUserNameKey}"];
                _repoName = _config[$"SourceWatcher:Github:{RegistryConstants.GithubRepoNameKey}"];
                _branch = _config[$"SourceWatcher:Github:{RegistryConstants.GithubBranchKey}"];
                _accessToken = _config[$"SourceWatcher:Github:{RegistryConstants.GithubAccessTokenKey}"];
            }

            _branch = !string.IsNullOrWhiteSpace(_branch) ? _branch : "master";
            _accessToken = _accessToken ?? string.Empty;
        }

        private void ValidateConfigurations()
        {
            if (string.IsNullOrWhiteSpace(_userName))
            {
                throw new ArgumentNullException("Github UserName");
            }

            if (string.IsNullOrWhiteSpace(_repoName))
            {
                throw new ArgumentNullException("Github RepoName");
            }
        }

        private void InitializeHttpClient()
        {
            _httpClient = new HttpClient
            {
                MaxResponseContentBufferSize = int.MaxValue,
                Timeout = TimeSpan.FromSeconds(30)
            };

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonContentType));
            _httpClient.DefaultRequestHeaders.Add(UserAgentHeaderName, _userName);
        }

        private void InitializeOctokitClient()
        {
            _octokitClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue(_userName))
            {
                Credentials = new Credentials(_accessToken)
            };
        }

        private string AppendQueryStringParams(string url)
        {
            var uriBuilder = new UriBuilder(url);
            var queryParams = HttpUtility.ParseQueryString(uriBuilder.Query);
            queryParams.Add("access_token", _accessToken);
            uriBuilder.Query = queryParams.ToString();
            return uriBuilder.ToString();
        }
    }
}
