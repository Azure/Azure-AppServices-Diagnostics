using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;
using Newtonsoft.Json;
using Diagnostics.ModelsAndUtils.Models.ChangeAnalysis;
using Diagnostics.DataProviders.TokenService;
using System.Collections.Generic;
using System.Net;
using System.Web;
using Diagnostics.DataProviders.DataProviderConfigurations;

namespace Diagnostics.DataProviders
{
    public class ChangeAnalysisClient : IChangeAnalysisClient
    {
        /// <summary>
        /// x-ms-client-object-id header to pass to Change Analysis endpoint.
        /// </summary>
        private string clientObjectIdHeader;

        /// <summary>
        /// For detectors loaded from Diagnose and Solve, pass x-ms-client-principal-name to Change Analysis endpoint.
        /// </summary>
        private string clientPrincipalNameHeader;

        /// <summary>
        /// ChangeAnalysis API endpoint.
        /// </summary>
        private string changeAnalysisEndPoint;

        /// <summary>
        /// ChangeAnalysis API version.
        /// </summary>
        private string apiVersion;

        private readonly Lazy<HttpClient> client = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
        );

        private HttpClient httpClient
        {
            get
            {
                return client.Value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeAnalysisClient"/> class.
        /// </summary>
        public ChangeAnalysisClient(ChangeAnalysisDataProviderConfiguration configuration, string clientObjectId, string clientPrincipalName = "")
        {
            clientObjectIdHeader = clientObjectId;
            clientPrincipalNameHeader = clientPrincipalName;
            changeAnalysisEndPoint = configuration.Endpoint;
            apiVersion = configuration.Apiversion;
        }

        /// <inheritdoc/>
        public async Task<List<ResourceChangesResponseModel>> GetChangesAsync(ChangeRequest changeRequest)
        {
            string requestUri = changeAnalysisEndPoint + $"changes?api-version={apiVersion}";
            object postBody = new
            {
                changeRequest.ResourceId,
                changeRequest.ChangeSetId
            };
            string jsonString = await PrepareAndSendRequest(requestUri, postBody, HttpMethod.Post);
            List<ResourceChangesResponseModel> resourceChangesResponse = JsonConvert.DeserializeObject<List<ResourceChangesResponseModel>>(jsonString);
            return resourceChangesResponse;
        }

        /// <inheritdoc/>
        public async Task<List<ChangeSetResponseModel>> GetChangeSetsAsync(ChangeSetsRequest changeSetsRequest)
        {
            string requestUri = changeAnalysisEndPoint + $"changesets?api-version={apiVersion}";
            object postBody = new
            {
                changeSetsRequest.ResourceId,
                StartTime = changeSetsRequest.StartTime.ToString(),
                EndTime = changeSetsRequest.EndTime.ToString()
            };
            string jsonString = await PrepareAndSendRequest(requestUri, postBody, HttpMethod.Post);
            List<ChangeSetResponseModel> changeSetsResponse = JsonConvert.DeserializeObject<List<ChangeSetResponseModel>>(jsonString);
            return changeSetsResponse;
        }

        /// <inheritdoc/>
        public async Task<ResourceIdResponseModel> GetResourceIdAsync(List<string> hostnames, string subscription)
        {
            string requestUri = changeAnalysisEndPoint + $"resourceId?api-version={apiVersion}";
            object requestBody = new
            {
                hostNames = hostnames,
                subscriptionId = subscription
            };
            string jsonString = await PrepareAndSendRequest(requestUri, requestBody, HttpMethod.Post);
            if (!string.IsNullOrWhiteSpace(jsonString))
            {
                ResourceIdResponseModel resourceIdResponse = JsonConvert.DeserializeObject<ResourceIdResponseModel>(jsonString);
                return resourceIdResponse;
            }

            return null;
        }

        /// <summary>
        /// Gets the last scan time stamp for a resource.
        /// </summary>
        /// <param name="armResourceUri">Azure Resource Uri.</param>
        /// <returns>Last scan information.</returns>
        public async Task<LastScanResponseModel> GetLastScanInformation(string armResourceUri)
        {
            string requestUri = changeAnalysisEndPoint + $"lastscan/{armResourceUri}?api-version={apiVersion}";
            string jsonString = await PrepareAndSendRequest(requestUri, httpMethod: HttpMethod.Get);
            return JsonConvert.DeserializeObject<LastScanResponseModel>(jsonString);
        }

        /// <summary>
        /// Checks if a subscription has registered the ChangeAnalysis RP.
        /// </summary>
        /// <param name="subscriptionId">Subscription Id.</param>
        public async Task<SubscriptionOnboardingStatus> CheckSubscriptionOnboardingStatus(string subscriptionId)
        {
            string requestUri = changeAnalysisEndPoint + $"Subscription/{subscriptionId}/onboardingstate?api-version={apiVersion}";
            try
            {
               string jsonString = await PrepareAndSendRequest(requestUri, httpMethod: HttpMethod.Get);
               var result = JsonConvert.DeserializeObject<SubscriptionOnboardingStatus>(jsonString);
               result.IsRegistered = true;
               return result;
            }
            catch (HttpRequestException httpexception)
            {
                if (httpexception.Data.Contains("Status Code") && (HttpStatusCode)httpexception.Data["Status Code"] == HttpStatusCode.NotFound)
                {
                    return new SubscriptionOnboardingStatus
                    {
                        IsRegistered = false
                    };
                }

                throw httpexception;
            }
        }

        /// <summary>
        /// Prepares httpwebrequest to <paramref name="requestUri"/> with <paramref name="postBody"/> as body of the request.
        /// </summary>
        /// <param name="requestUri">Change Analysis Request URI</param>
        /// <param name="postBody">Body of the request</param>
        /// <returns>JSON string received from <paramref name="requestUri"/>.</returns>
        private async Task<string> PrepareAndSendRequest(string requestUri, object postBody = null, HttpMethod httpMethod = null)
        {
            HttpMethod requestedHttpMethod = httpMethod == null ? HttpMethod.Post : httpMethod;
            HttpRequestMessage requestMessage = new HttpRequestMessage(requestedHttpMethod, requestUri);
            string authToken = await ChangeAnalysisTokenService.Instance.GetAuthorizationTokenAsync();

            // Add required headers.
            requestMessage.Headers.Add("Authorization", authToken);
            requestMessage.Headers.Add("x-ms-client-object-id", clientObjectIdHeader);

            // For requests coming from Diagnose and Solve, add x-ms-principal-name header.
            if (!string.IsNullOrWhiteSpace(clientPrincipalNameHeader))
            {
               requestMessage.Headers.Add("x-ms-principal-name", clientPrincipalNameHeader);
            }

            if (postBody != null)
            {
               requestMessage.Content = new StringContent(JsonConvert.SerializeObject(postBody), Encoding.UTF8, "application/json");
            }

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(DataProviderConstants.DefaultTimeoutInSeconds));
            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, cancellationTokenSource.Token);
            string content = await responseMessage.Content.ReadAsStringAsync();
            if (responseMessage.StatusCode == HttpStatusCode.NotFound)
            {
                HttpRequestException ex = new HttpRequestException($"Status Code : {responseMessage.StatusCode}, Content : {content}");
                ex.Data.Add("Status Code", responseMessage.StatusCode);
                throw ex;
            }
            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Status Code: {responseMessage.StatusCode}, Content: {content}");
            }

            return content;
        }
    }
}
