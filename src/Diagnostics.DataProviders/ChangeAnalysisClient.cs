using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;
using Newtonsoft.Json;
using Diagnostics.ModelsAndUtils.Models.ChangeAnalysis;

namespace Diagnostics.DataProviders
{
    public class ChangeAnalysisClient : IChangeAnalysisClient
    {
        /// <summary>
        /// x-ms-client-object-id header to pass to Change Analysis endpoint.
        /// </summary>
        private string clientObjectId;

        /// <summary>
        /// For detectors loaded from Diagnose and Solve, pass x-ms-client-principal-name to Change Analysis endpoint.
        /// </summary>
        private string clientPrincipalName;

        private readonly string changeAnalysisEndPoint = "https://changeanalysis-dataplane-dev.azurewebsites.net/providers/microsoft.changeanalysis/";

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
        public ChangeAnalysisClient()
        {

        }

        /// <inheritdoc/>
        public async void GetChangesAsync(ChangeRequest changeRequest)
        {
            string requestUri = changeAnalysisEndPoint + "changes?api-version=2019-04-01-preview";
            object postBody = new
            {
                changeRequest.ResourceId,
                changeRequest.ChangeSetId
            };
            string jsonString = await PrepareAndSendRequest(requestUri, postBody);
        }

        /// <inheritdoc/>
        public async void GetChangeSetsAsync(ChangeSetsRequest changeSetsRequest)
        {
            string requestUri = changeAnalysisEndPoint + "changesets?api-version=2019-04-01-preview";
            object postBody = new
            {
                changeSetsRequest.ResourceId,
                StartTime = changeSetsRequest.StartTime.ToString(),
                EndTime = changeSetsRequest.EndTime.ToString()
            };
            string jsonString = await PrepareAndSendRequest(requestUri, postBody);
        }

        /// <inheritdoc/>
        public async void GetResoureceIdAsync(string[] hostnames, string subscription)
        {
            string requestUri = changeAnalysisEndPoint + "resourceId?api-version=2019-04-01-preview";
            object requestBody = new
            {
                hostNames = hostnames,
                subscriptionId = subscription
            };
            string jsonString = await PrepareAndSendRequest(requestUri, requestBody);
        }

        /// <summary>
        /// Prepares httpwebrequest to <paramref name="requestUri"/> with <paramref name="postBody"/> as body of the request.
        /// </summary>
        /// <param name="requestUri">Change Analysis Request URI</param>
        /// <param name="postBody">Body of the request</param>
        /// <returns>JSON string received from <paramref name="requestUri"/>.</returns>
        private async Task<string> PrepareAndSendRequest(string requestUri, object postBody)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);

            // Add required headers
            requestMessage.Headers.Add("Authorization", "");
            requestMessage.Headers.Add("x-ms-client-object-id", clientObjectId);
            // For requests coming from Diagnose and Solve 
            requestMessage.Headers.Add("x-ms-principal-name", clientPrincipalName);
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(postBody), Encoding.UTF8, "application/json");
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(DataProviderConstants.DefaultTimeoutInSeconds));
            try
            {
                HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, cancellationTokenSource.Token);
                string content = await responseMessage.Content.ReadAsStringAsync();
                if (!responseMessage.IsSuccessStatusCode)
                {
                    throw new Exception(content);
                }

                return content;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
