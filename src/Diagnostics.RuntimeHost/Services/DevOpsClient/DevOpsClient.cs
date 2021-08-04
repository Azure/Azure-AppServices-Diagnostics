using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Diagnostics.RuntimeHost.Services.DevOpsClient
{
    public interface IDevOpsClient : IDisposable, IRepoClient
    {
        string AccessToken { get; }

        void setAccessToken(string token);
    }


    class userIdPair
    {
        public string userName;
        public string userId;

        public userIdPair(string name, string Id)
        {
            userName = name;
            userId = Id;
        }
    }

    class DevOpsClient : IDevOpsClient
    {
        private DevOpsRequestBodyAssistant devOpsRBA = new DevOpsRequestBodyAssistant();
        private string _accessToken;

        public string AccessToken => _accessToken;

        private async Task<string> getLastObjectIdAsync(string organization, string project, string repositoryId, string branch)
        {
            HttpResponseMessage catchResponse = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/commits?searchCriteria.$top=1&searchCriteria.itemVersion.version={branch}&api-version=6.1-preview.1");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", _accessToken))));

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();

                        JObject parsedResponse = JObject.Parse(await response.Content.ReadAsStringAsync());
                        var valueObject = parsedResponse.SelectToken("$.value");
                        var id = valueObject[0].SelectToken("$.commitId");
                        return id.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        private async Task<List<string>> getUserIdsAsync(string organization, IList<string> aliases)
        {
            HttpResponseMessage catchResponse = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://vsaex.dev.azure.com/{organization}/_apis/userentitlements?api-version=6.1-preview.3");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", _accessToken))));

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();

                        JObject parsedResponse = JObject.Parse(await response.Content.ReadAsStringAsync());
                        var members = parsedResponse.SelectToken("$.members");
                        List<userIdPair> userIdPairs = new List<userIdPair>();
                        foreach (var member in members)
                        {
                            var id = member.SelectToken("$.id");
                            var user = member.SelectToken("$.user").SelectToken("$.directoryAlias");
                            userIdPairs.Add(new userIdPair(user.ToString(), id.ToString()));
                        }

                        List<string> Ids = new List<string>();
                        foreach (var user in userIdPairs)
                        {
                            if (aliases.Contains(user.userName))
                            {
                                Ids.Add(user.userId);
                            }
                        }

                        return Ids;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public void setAccessToken(string token)
        {
            _accessToken = token;
        }

        public async Task<HttpResponseMessage> getOrgMembersAsync(string organization)
        {
            HttpResponseMessage catchResponse = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://vsaex.dev.azure.com/{organization}/_apis/userentitlements?api-version=6.1-preview.3");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", _accessToken))));

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        catchResponse = response;
                        response.EnsureSuccessStatusCode();
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return catchResponse;
            }
        }

        public async Task<HttpResponseMessage> getCommitsAsync(string organization, string project, string repositoryId, string branch, int maxResults = 100)
        {
            HttpResponseMessage catchResponse = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/commits?searchCriteria.$top={maxResults}&searchCriteria.itemVersion.version={branch}&api-version=6.1-preview.1");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", _accessToken))));

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        catchResponse = response;
                        response.EnsureSuccessStatusCode();
                        return response;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return catchResponse;
            }
        }

        public async Task<HttpResponseMessage> getCommitsAsync(string organization, string project, string repositoryId, int maxResults = 100)
        {
            HttpResponseMessage catchResponse = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/commits?searchCriteria.$top={maxResults}&api-version=6.1-preview.1");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", _accessToken))));

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        catchResponse = response;
                        response.EnsureSuccessStatusCode();
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return catchResponse;
            }
        }

        public async Task<HttpResponseMessage> getDetectorCodeAsync(string organization, string project, string repositoryId, string detectorPath)
        {
            HttpResponseMessage catchResponse = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/items?path={WebUtility.UrlEncode(detectorPath)}&api-version=6.1-preview.1");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", _accessToken))));

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        catchResponse = response;
                        response.EnsureSuccessStatusCode();
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return catchResponse;
            }


        }

        public async Task<HttpResponseMessage> getPullRequestAsync(string organization, string project, string repositoryId, string pullRequestId)
        {
            HttpResponseMessage catchResponse = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullrequests/{pullRequestId}?api-version=6.1-preview.1");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", _accessToken))));

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        catchResponse = response;
                        response.EnsureSuccessStatusCode();
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return catchResponse;
            }
        }

        public async Task<HttpResponseMessage> getPullRequestsAsync(string organization, string project, string repositoryId)
        {
            HttpResponseMessage catchResponse = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullrequests?api-version=6.1-preview.1");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", _accessToken))));

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        catchResponse = response;
                        response.EnsureSuccessStatusCode();
                        return response;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return catchResponse;
            }
        }

        public async Task<HttpResponseMessage> makePullRequestAsync(string organization, string project, string repositoryId, string sourceBranch, string targetBranch, string title, string description, string reviewersString)
        {
            List<string> reviewers = reviewersString.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            string source = $"refs/heads/{sourceBranch}";
            string target = $"refs/heads/{targetBranch}";

            List<string> reviewerIds = await getUserIdsAsync(organization, reviewers);

            HttpResponseMessage catchResponse = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullrequests?api-version=6.1-preview.1");
                    request.Headers.Add("Authorization", $"Bearer {_accessToken}");
                    var requestBody = devOpsRBA.generatePRRequestBody(source, target, title, description, reviewerIds);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", _accessToken))));

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        catchResponse = response;
                        response.EnsureSuccessStatusCode();
                        return response;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return catchResponse;
            }
        }

        public async Task<HttpResponseMessage> pushChangesAsync(string organization, string project, string repositoryId, string branch, string localPath, string repoPath, string comment, string changeType)
        {
            string name = $"refs/heads/{branch}";
            string oldObjectId = await getLastObjectIdAsync(organization, project, repositoryId, branch);

            HttpResponseMessage catchResponse = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/pushes?api-version=6.1-preview.2");
                    request.Headers.Add("Authorization", $"Bearer {_accessToken}");
                    var requestBody = devOpsRBA.generatePushRequestBody(name, oldObjectId, repoPath, localPath, changeType, comment);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", _accessToken))));

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();
                        return response;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return catchResponse;
            }
        }

        public async Task<HttpResponseMessage> getRepositoriesAsync(string organization)
        {
            HttpResponseMessage catchResponse = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://dev.azure.com/{organization}/_apis/git/repositories?api-version=6.1-preview.1");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", _accessToken))));

                    using (HttpResponseMessage response = await client.SendAsync(requestMessage))
                    {
                        catchResponse = response;
                        response.EnsureSuccessStatusCode();
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return catchResponse;
            }
        }

        public async Task<HttpResponseMessage> getPullRequestCommentsAsync(string organization, string project, string repositoryId, string pullRequestId)
        {
            HttpResponseMessage catchResponse = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullRequests/{pullRequestId}/threads?api-version=6.1-preview.1");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", _accessToken))));

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        catchResponse = response;
                        response.EnsureSuccessStatusCode();
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return catchResponse;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
