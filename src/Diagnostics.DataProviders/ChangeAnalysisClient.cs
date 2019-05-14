using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.DataProviders.TokenService;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models.ChangeAnalysis;
using Newtonsoft.Json;

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

        private const string ExceptionStatusCode = "StatusCode";

        private string requestId;

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
        public ChangeAnalysisClient(ChangeAnalysisDataProviderConfiguration configuration, string requestTrackingId, string clientObjectId, string clientPrincipalName = "")
        {
            clientObjectIdHeader = clientObjectId;
            clientPrincipalNameHeader = clientPrincipalName;
            changeAnalysisEndPoint = configuration.Endpoint;
            apiVersion = configuration.Apiversion;
            requestId = requestTrackingId;
        }

        /// <inheritdoc/>
        public async Task<List<ResourceChangesResponseModel>> GetChangesAsync(ChangeRequest changeRequest)
        {
            try
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
            catch (HttpRequestException httpException)
            {
                // Its possible that users dont have access to view the change details, log the exception and send empty list of change details.
                string message = $"HttpRequestException in GetChangesAsync, Message: {httpException.Message} ";
                if (httpException.Data.Contains(ExceptionStatusCode))
                {
                    message += $"Status Code: {httpException.Data[ExceptionStatusCode]}";
                }

                DiagnosticsETWProvider.Instance.LogDataProviderMessage(requestId, "ChangeAnalysisClient", message);
                return new List<ResourceChangesResponseModel>();
            }
        }

        /// <inheritdoc/>
        public async Task<List<ChangeSetResponseModel>> GetChangeSetsAsync(ChangeSetsRequest changeSetsRequest)
        {
            try
            {
                string requestUri = changeAnalysisEndPoint + $"changesets?api-version={apiVersion}";
                changeSetsRequest.StartTime = changeSetsRequest.StartTime.ToUniversalTime();
                changeSetsRequest.EndTime = changeSetsRequest.EndTime.ToUniversalTime();
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
            catch (HttpRequestException httpException)
            {
                string message = $"HttpRequestException in GetChangeSetsAsync, Message: {httpException.Message} ";
                if (httpException.Data.Contains(ExceptionStatusCode))
                {
                    message += $"Status Code: {httpException.Data[ExceptionStatusCode]}";
                }

                DiagnosticsETWProvider.Instance.LogDataProviderMessage(requestId, "ChangeAnalysisClient", message);
                return new List<ChangeSetResponseModel>();
            }
        }

        /// <inheritdoc/>
        public async Task<ResourceIdResponseModel> GetResourceIdAsync(List<string> hostnames, string subscription)
        {
            try
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

                return new ResourceIdResponseModel();
            }
            catch (HttpRequestException httpException)
            {
                string message = $"HttpRequestException in GetResourceIdAsync, Message: {httpException.Message} ";
                if (httpException.Data.Contains(ExceptionStatusCode))
                {
                    message += $"Status Code: {httpException.Data[ExceptionStatusCode]}";
                }

                DiagnosticsETWProvider.Instance.LogDataProviderMessage(requestId, "ChangeAnalysisClient", message);
                return new ResourceIdResponseModel();
            }
        }

        /// <summary>
        /// Gets the last scan time stamp for a resource.
        /// </summary>
        /// <param name="armResourceUri">Azure Resource Uri.</param>
        /// <returns>Last scan information.</returns>
        public async Task<LastScanResponseModel> GetLastScanInformation(string armResourceUri)
        {
            try
            {
                string requestUri = changeAnalysisEndPoint + $"lastscan/{armResourceUri}?api-version={apiVersion}";
                string jsonString = await PrepareAndSendRequest(requestUri, httpMethod: HttpMethod.Get);
                return JsonConvert.DeserializeObject<LastScanResponseModel>(jsonString);
            }
            catch (HttpRequestException httpException)
            {
                string message = $"HttpRequestException in GetLastScanInformation, Message: {httpException.Message} ";
                if (httpException.Data.Contains(ExceptionStatusCode))
                {
                    message += $"Status Code: {httpException.Data[ExceptionStatusCode]}";
                }

                DiagnosticsETWProvider.Instance.LogDataProviderMessage(requestId, "ChangeAnalysisClient", message);
                return new LastScanResponseModel
                {
                    ResourceId = string.Empty,
                    TimeStamp = string.Empty,
                    Source = string.Empty
                };
            }
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
                string message = $"HttpRequestException in CheckSubscriptionOnboardingStatus, Message: {httpexception.Message} ";
                if (httpexception.Data.Contains(ExceptionStatusCode))
                {
                    message += $"Status Code: {httpexception.Data[ExceptionStatusCode]}";
                }

                DiagnosticsETWProvider.Instance.LogDataProviderMessage(requestId, "ChangeAnalysisClient", message);
                if (httpexception.Data.Contains(ExceptionStatusCode) && (HttpStatusCode)httpexception.Data[ExceptionStatusCode] == HttpStatusCode.NotFound)
                {
                    return new SubscriptionOnboardingStatus
                    {
                        IsRegistered = false
                    };
                }

                throw;
            }
        }

        /// <summary>
        /// Submits scan request to Change Analysis RP or checks scan status.
        /// </summary>
        /// <param name="resourceId">Azure resource id</param>
        /// <param name="scanAction">Scan action: It is "submitscan" or "checkscan".</param>
        /// <returns>Contains info about the scan request with submissions state and time.</returns>
        public async Task<ChangeScanModel> ScanActionRequest(string resourceId, string scanAction)
        {
            try
            {
                string requestUri = changeAnalysisEndPoint + $"{scanAction}/{resourceId}?api-version={apiVersion}";
                HttpMethod httpMethod = scanAction.Equals("checkscan") ? HttpMethod.Get : HttpMethod.Post;
                string jsonString = await PrepareAndSendRequest(requestUri, httpMethod: httpMethod);
                return JsonConvert.DeserializeObject<ChangeScanModel>(jsonString);
            }
            catch (HttpRequestException httpexception)
            {
                string message = $"HttpRequestException in ScanActionRequest, Message: {httpexception.Message}, Scan Action: {scanAction}  ";
                if (httpexception.Data.Contains(ExceptionStatusCode))
                {
                    message += $"Status Code: {httpexception.Data[ExceptionStatusCode]}";
                }

                DiagnosticsETWProvider.Instance.LogDataProviderMessage(requestId, "ChangeAnalysisClient", message);
                // 404 NotFound mean there are no active requests.
                if (httpexception.Data.Contains(ExceptionStatusCode) && (HttpStatusCode)httpexception.Data[ExceptionStatusCode] == HttpStatusCode.NotFound)
                {
                    return new ChangeScanModel
                    {
                        OperationId = string.Empty,
                        State = "No active requests",
                        SubmissionTime = null,
                        CompletionTime = null
                    };
                }

                if(httpexception.Data.Contains(ExceptionStatusCode) && (HttpStatusCode)httpexception.Data[ExceptionStatusCode] == HttpStatusCode.Forbidden)
                {
                    return new ChangeScanModel
                    {
                        OperationId = string.Empty,
                        State = "NotEnabled",
                        SubmissionTime = null,
                        CompletionTime = null
                    };
                }

                return new ChangeScanModel
                {
                    OperationId = string.Empty,
                    State = "",
                    SubmissionTime = null,
                    CompletionTime = null
                };
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

            // For requests coming from Diagnose and Solve, add x-ms-client-principal-name header.
            if (!string.IsNullOrWhiteSpace(clientPrincipalNameHeader))
            {
               requestMessage.Headers.Add("x-ms-client-principal-name", clientPrincipalNameHeader);
            }

            if (postBody != null)
            {
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(postBody), Encoding.UTF8, "application/json");
            }

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(DataProviderConstants.DefaultTimeoutInSeconds));
            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, cancellationTokenSource.Token);
            string content = await responseMessage.Content.ReadAsStringAsync();
            if (!responseMessage.IsSuccessStatusCode)
            {
                HttpRequestException ex = new HttpRequestException($"Status Code : {responseMessage.StatusCode}, Content : {content}");
                ex.Data.Add(ExceptionStatusCode, responseMessage.StatusCode);
                throw ex;
            }

            return content;
        }
    }
}
