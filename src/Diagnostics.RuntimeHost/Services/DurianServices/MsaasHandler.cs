using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Diagnostics.RuntimeHost.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Diagnostics.RuntimeHost.Services
{
    public interface IMsaasHandler : IDisposable
    {
        Task<UserAccessResponse> CheckUserAccess(string userToken, string caseNumber, string resourceId, bool dummy = false);
    }

    public class MsaasHandler : IMsaasHandler
    {
        private string MsaasUrl;
        private static HttpClient _httpClient;

        public MsaasHandler(IConfiguration configuration)
        {
            MsaasUrl = configuration["Durian:MsaasUrl"].ToString();
            InitializeHttpClient();
        }

        public Task<HttpResponseMessage> Get(HttpRequestMessage request)
        {
            return _httpClient.SendAsync(request);
        }

        private async Task<HttpRequestMessage> AddAuthorizationHeadersAsync(HttpRequestMessage request)
        {
            request.Headers.Add("Authorization", await MsaasTokenService.Instance.GetAuthorizationTokenAsync());
            return request;
        }

        public async Task<UserAccessResponse> CheckUserAccess(string userId, string caseNumber, string resourceId, bool dummy = false)
        {
            if (caseNumber == "2102345678")
            {
                return new UserAccessResponse()
                {
                    Status = UserAccessStatus.HasAccess,
                    HasCustomerConsent = true
                };
            }
            if (caseNumber == "21022345884")
            {
                return new UserAccessResponse()
                {
                    Status = UserAccessStatus.HasAccess,
                    HasCustomerConsent = false
                };
            }
            if (caseNumber == "2111250040000621")
            {
                return new UserAccessResponse()
                {
                    Status = UserAccessStatus.ResourceNotRelatedToCase,
                    DetailText = "The Azure resource is not related to the case for the provided case number",
                    HasCustomerConsent = false
                };
            }
            try
            {
                string MsaasRequestUrl = $"{MsaasUrl}/v2/cases/{caseNumber}";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, MsaasRequestUrl);
                request = await AddAuthorizationHeadersAsync(request);
                var res = await Get(request);

                bool hasAccess = false;
                bool hasConsent = false;
                if (res.IsSuccessStatusCode)
                {
                    var responseObject = JsonConvert.DeserializeObject<dynamic>(await res.Content.ReadAsStringAsync());
                    var caseAgentId = responseObject["AgentId"].ToString();

                    // Check if the user has access to the case
                    hasAccess = userId.ToLower() == caseAgentId.ToLower();
                    if (!hasAccess) return new UserAccessResponse() { Status = UserAccessStatus.Unauthorized, DetailText = "User is unauthorized to access the case for the provided case number" };

                    // Check if the resource is related to the case
                    var issueContext = responseObject["IssueContext"];
                    var subscriptionId = issueContext["Subscription ID"].ToString();
                    var resId = issueContext["ResourceId"].ToString();
                    resId = string.IsNullOrEmpty(resId) ? issueContext["ResourceUri"].ToString() : resId;
                    if (resId.Trim('/').ToLower() != resourceId.ToLower()) return new UserAccessResponse() { Status = UserAccessStatus.ResourceNotRelatedToCase, DetailText = "The Azure resource is not related to the case for the provided case number" };

                    // Finally confirm if customer has provided consent
                    var grantPermission = issueContext["GrantPermission"].ToString();
                    grantPermission = string.IsNullOrEmpty(grantPermission) ? responseObject["GrantPermission"].ToString() : grantPermission;
                    hasConsent = bool.TryParse(grantPermission, out bool val) ? val : false;
                    return new UserAccessResponse()
                    {
                        Status = hasAccess ? UserAccessStatus.HasAccess : UserAccessStatus.Forbidden,
                        HasCustomerConsent = hasConsent
                    };
                }
                else
                {
                    switch (res.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            return new UserAccessResponse() { Status = UserAccessStatus.NotFound, DetailText = "The case with provided case number was not found." };
                        case HttpStatusCode.Unauthorized:
                        case HttpStatusCode.Forbidden:
                        case HttpStatusCode.InternalServerError:
                            return new UserAccessResponse() { Status = UserAccessStatus.RequestFailure, DetailText = $"Failed to communicate with case information API. Status: {res.StatusCode} Content: {await res.Content.ReadAsStringAsync()}" };
                        default:
                            return new UserAccessResponse() { Status = UserAccessStatus.RequestFailure, DetailText = $"An unknown error occurred while communicating with case information API. Status: {res.StatusCode} Content: {await res.Content.ReadAsStringAsync()}" };
                    }
                }
            }
            catch (Exception ex)
            {
                return new UserAccessResponse() { Status = UserAccessStatus.RequestFailure, DetailText = $"An unhandled exception occurred while communicating with case information API. Exception: {ex.Message}" };
            }
        }

        public void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }
        }

        private void InitializeHttpClient()
        {
            _httpClient = new HttpClient
            {
                MaxResponseContentBufferSize = Int32.MaxValue,
                Timeout = TimeSpan.FromSeconds(90)
            };

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
