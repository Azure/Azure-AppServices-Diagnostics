using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Diagnostics.RuntimeHost.Services;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Diagnostics.RuntimeHost.Utilities
{
    public static class GraphAPIUtils
    {
        private static readonly Lazy<HttpClient> Client = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        });

        private static HttpClient HttpClientObj
        {
            get
            {
                return Client.Value;
            }
        }

        /// <summary>
        /// Validates if a user belongs to a security or distribution group.
        /// </summary>
        /// <param name="userAlias">user Alias</param>
        /// <param name="groupObjectId">group object id.</param>
        /// <returns>True, if user is part of the group.</returns>
        public static async Task<string[]> CheckUserGroupMemberships(string userAlias, string[] groupIds, bool dummy = false)
        {
            if (dummy)
            {
                return (new List<string>() { "cssgroup" }).ToArray();
            }
            string graphUrl = GraphConstants.GraphApiCheckMemberGroupsFormat;
            var requestUrl = string.Format(graphUrl, userAlias);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            Dictionary<string, Array> requestParams = new Dictionary<string, Array>();
            requestParams.Add("groupIds", groupIds);
            string authorizationToken = await GraphAPITokenService.Instance.GetAuthorizationTokenAsync();
            request.Headers.Add("Authorization", authorizationToken);
            request.Content = new StringContent(JsonConvert.SerializeObject(requestParams), Encoding.UTF8, "application/json");

            HttpResponseMessage responseMsg = await HttpClientObj.SendAsync(request);
            var res = await responseMsg.Content.ReadAsStringAsync();
            dynamic groupIdsResponse = JsonConvert.DeserializeObject<dynamic>(res);
            string[] groupIdsReturned = groupIdsResponse.value.ToObject<string[]>();

            return groupIdsReturned;
        }

        /// <summary>
        /// Gets User Id from Auth token.
        /// </summary>
        /// <param name="authorizationToken">Auth token.</param>
        /// <returns>User Id.</returns>
        public static string GetUserIdFromToken(string authorizationToken)
        {
            string userId = string.Empty;
            if (string.IsNullOrWhiteSpace(authorizationToken))
            {
                throw new ArgumentNullException(nameof(authorizationToken));
            }

            string accessToken = authorizationToken;
            if (authorizationToken.ToLower().Contains("bearer "))
            {
                accessToken = authorizationToken.Split(" ")[1];
            }

            var token = new JwtSecurityToken(accessToken);
            if (token.Payload.TryGetValue("upn", out object upn))
            {
                userId = upn.ToString();
            }

            return userId;
        }
    }
}
