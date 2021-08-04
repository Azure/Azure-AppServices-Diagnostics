using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;

namespace Diagnostics.DataProviders.TokenService
{
    /*[DataContract]
    public class TokenModel
    {
        [DataMember(Name = "access_token")]
        public String AccessToken { get; set; }

        [DataMember(Name = "token_type")]
        public String TokenType { get; set; }

        [DataMember(Name = "refresh_token")]
        public String RefreshToken { get; set; }

        [DataMember(Name = "expires_in")]
        public int ExpiresIn { get; set; }

        public bool IsPending { get; set; }
    }

    public class DevOpsTokenService
    {
        private static readonly HttpClient s_httpClient = new HttpClient();
        private static readonly Dictionary<Guid, TokenModel> s_authorizationRequests = new Dictionary<Guid, TokenModel>();
        private static readonly Lazy<DevOpsTokenService> instance = new Lazy<DevOpsTokenService>(() => new DevOpsTokenService());
        public static DevOpsTokenService Instance => instance.Value;

        protected override string TokenServiceName { get; set; }

        public ActionResult Authorize()
        {
            Guid state = Guid.NewGuid();

            s_authorizationRequests[state] = new TokenModel() { IsPending = true };

            return new RedirectResult(GetAuthorizationUrl(state.ToString()));
        }

        private static String GetAuthorizationUrl(String state)
        {
            UriBuilder uriBuilder = new UriBuilder(ConfigurationManager.AppSettings["AuthUrl"]);
            var queryParams = HttpUtility.ParseQueryString(uriBuilder.Query ?? String.Empty);

            queryParams["client_id"] = ConfigurationManager.AppSettings["ClientAppId"];
            queryParams["response_type"] = "Assertion";
            queryParams["state"] = state;
            queryParams["scope"] = ConfigurationManager.AppSettings["Scope"];
            queryParams["redirect_uri"] = ConfigurationManager.AppSettings["CallbackUrl"];

            uriBuilder.Query = queryParams.ToString();

            return uriBuilder.ToString();
        }

        public void Initialize(K8SELogAnalyticsDataProviderConfiguration k8SELogAnalyticsDataProviderConfiguration)
        {
            TokenServiceName = "DevOpsTokenRefresh";

            StartTokenRefresh();
        }
    }*/
}
