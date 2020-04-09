using System;
using Octokit;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using SourceWatcherFuncApp.Entities;

namespace SourceWatcherFuncApp.Services
{
    public interface IGithubService
    {
        Task<Reference> CreateBranchIfNotExists();

        Task<GithubEntry[]> DownloadGithubDirectories(string branchName = "", string branchdownloadUrl = "");
        Task<Stream> GetFileContentStream(string url);
        Task<DetectorEntity> GetFileContentJson(string url);
        Task<T> GetFileContentByType<T>(string url);

        Task<DateTime> GetCommitDate(string path);
    }

    public class GithubService: IGithubService
    {
        private static HttpClient httpClient = new HttpClient();

        private GitHubClient octokitClient;

        private string baseBranch;

        private string targetBranch;

        private string githubUserName;

        private string repoName;
        public GithubService(IConfigurationRoot config)
        {

            githubUserName = config["Github:UserName"];
            repoName = config["Github:RepoName"];
            baseBranch = config["Github:Branch"];
            var AccessToken = config["Github:AccessToken"];
            targetBranch = config["Github:TargetBranch"];

            octokitClient = new GitHubClient(new Octokit.ProductHeaderValue(githubUserName))
            {
                Credentials = new Credentials(AccessToken)
            };

            // Initialize HttpClient

            httpClient.DefaultRequestHeaders.Add("Authorization", $@"token {AccessToken}");
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("User-Agent", "sourcewatcherfuncapp");
        } 

        public async Task<Reference> CreateBranchIfNotExists()
        {

            // Check if branch exists

            var gitHubbasebranch = await octokitClient.Git.Reference.Get(githubUserName, repoName, $"heads/{this.baseBranch}");
            Reference targetGithubBranch = null;

            try
            {
                targetGithubBranch = await octokitClient.Git.Reference.Get(githubUserName, repoName, $"heads/{this.targetBranch}");
            }
            catch (Exception ex)
            {
                if (ex.ToString().Contains("Not found"))
                {
                    targetGithubBranch = await Octokit.Helpers.ReferenceExtensions.CreateBranch(octokitClient.Git.Reference, githubUserName, repoName, targetBranch, gitHubbasebranch);
                }
            }
            return targetGithubBranch;
        }

        public async Task<GithubEntry[]> DownloadGithubDirectories(string branchName = "", string branchdownloadUrl = "")
        {
            var downloadUrl = !string.IsNullOrWhiteSpace(branchName) ? $"https://api.github.com/repos/{githubUserName}/{repoName}/contents?ref={branchName}" : branchdownloadUrl;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);

            var response = await httpClient.SendAsync(request);
            var jsonString = await response.Content.ReadAsStringAsync();
            var githubDirectories = JsonConvert.DeserializeObject<GithubEntry[]>(jsonString);

            return githubDirectories;
           
        }

        public async Task<DetectorEntity> GetFileContentJson(string url)
        {
            var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
            var responsestring = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DetectorEntity>(responsestring);
        }

        public async Task<Stream> GetFileContentStream(string url)
        {
            var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
            var stream = await response.Content.ReadAsStreamAsync();
            return stream;
        }

        public async Task<T> GetFileContentByType<T>(string url)
        {
            var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
            var responseContent = await response.Content.ReadAsStringAsync();

            if (typeof(T).Equals(typeof(string)))
            {
                return (T)(object)responseContent;
            }
            else
            {

                T value;
                try
                {
                    value = JsonConvert.DeserializeObject<T>(responseContent);
                }
                catch (JsonSerializationException serializeException)
                {
                    throw new JsonSerializationException($"Failed to serialize response {responseContent}", serializeException);
                }

                return value;
            }
        }

        public async Task<DateTime> GetCommitDate(string path)
        {
            if(path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            var commitsForFile = await octokitClient.Repository.Commit.GetAll(githubUserName, repoName, new CommitRequest { Path = path, Sha = baseBranch });
            var mostRecentCommit = commitsForFile[0];
            var authorDate = mostRecentCommit.Commit.Author.Date;
            return authorDate.DateTime.ToUniversalTime();
        }
        
    }
}
