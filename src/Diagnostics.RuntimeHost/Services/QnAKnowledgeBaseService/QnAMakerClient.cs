using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Models.QnAModels;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services
{
    public class QnAMakerClient : IDisposable
    {
        private string _ocpApimSubscriptionKey;
        private string _knowledgeBaseEndpoint;
        private string _operationsEndpoint;
        private HttpClient _httpClient;
        private IHostingEnvironment _env;
        private readonly IConfiguration _config;

        public QnAMakerClient(IHostingEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _config = configuration;
            LoadConfigurations();
            InitializeHttpClient();
        }

        public async Task<List<QnAPair>> GetAllQnAPairsInKB(string kbId)
        {
            if (string.IsNullOrWhiteSpace(kbId))
            {
                throw new ArgumentNullException("kbId");
            }

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $@"{_knowledgeBaseEndpoint}\{kbId}\Test\qna");
            var response = await this._httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(errorContent);
            }

            JToken qnaContent = await response.Content.ReadAsAsyncCustom<JToken>();
            List<QnAPair> qnaPairs = JsonConvert.DeserializeObject<List<QnAPair>>(qnaContent["qnaDocuments"].ToString());
            return qnaPairs;
        }

        public async Task UpdateAndTrainKB(string kbId, List<QnAPair> qnaPairsToBeAdded, List<QnAPair> qnaPairsToBeDeleted, List<QnAPairUpdateModel> qnaPairsToBeUpdated)
        {
            if (string.IsNullOrWhiteSpace(kbId))
            {
                throw new ArgumentNullException("kbId");
            }

            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), $@"{_knowledgeBaseEndpoint}\{kbId}");

            UpdateKBPostBody postBody = new UpdateKBPostBody();

            if (qnaPairsToBeAdded != null && qnaPairsToBeAdded.Any())
            {
                postBody.add = new AddQnAPairBody()
                {
                    qnaList = qnaPairsToBeAdded
                };
            }

            if (qnaPairsToBeDeleted != null && qnaPairsToBeDeleted.Any())
            {
                postBody.delete = new DeleteQnaPairBody()
                {
                    ids = qnaPairsToBeDeleted.Select(x => x.Id).ToList()
                };
            }

            if (qnaPairsToBeUpdated != null && qnaPairsToBeUpdated.Any())
            {
                postBody.update = new UpdateQnaPairBody()
                {
                    qnaList = qnaPairsToBeUpdated
                };
            }

            request.Content = new StringContent(JsonConvert.SerializeObject(postBody), Encoding.UTF8, "application/json");

            var response = await this._httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(errorContent);
            }

            QnAOperation operationDetails = await response.Content.ReadAsAsyncCustom<QnAOperation>();

            while ((operationDetails.OperationState.ToLower().Equals("running")
                || (operationDetails.OperationState.ToLower().Equals("notstarted"))))
            {
                await Task.Delay(5000);
                operationDetails = await GetOperationDetails(operationDetails.OperationId);
            }
        }

        public async Task PublishKB(string kbId)
        {
            if (string.IsNullOrWhiteSpace(kbId))
            {
                throw new ArgumentNullException("kbId");
            }

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{_knowledgeBaseEndpoint}/{kbId}");

            var response = await this._httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(errorContent);
            }
        }

        public void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }
        }

        private async Task<QnAOperation> GetOperationDetails(string operationId)
        {
            if (string.IsNullOrWhiteSpace(operationId))
            {
                throw new ArgumentNullException("operationId");
            }

            HttpRequestMessage reqeust = new HttpRequestMessage(HttpMethod.Get, $"{_operationsEndpoint}/{operationId}");
            var response = await _httpClient.SendAsync(reqeust);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(errorContent);
            }

            QnAOperation operation = await response.Content.ReadAsAsyncCustom<QnAOperation>();
            return operation;
        }

        private void LoadConfigurations()
        {
            _knowledgeBaseEndpoint = "https://westus.api.cognitive.microsoft.com/qnamaker/v4.0/knowledgebases";
            _operationsEndpoint = "https://westus.api.cognitive.microsoft.com/qnamaker/v4.0/operations";

            if (_env.IsProduction())
            {
                _ocpApimSubscriptionKey = (string)Registry.GetValue(RegistryConstants.QnAKnowledgeBaseRegistryPath, RegistryConstants.QnAServiceOcpApimSubscriptionKey, string.Empty);
            }
            else
            {
                _ocpApimSubscriptionKey = (_config[$"QnAKnowledgeBase:{RegistryConstants.QnAServiceOcpApimSubscriptionKey}"]).ToString();
            }

            if (string.IsNullOrWhiteSpace(_ocpApimSubscriptionKey))
            {
                throw new Exception("OcpApimSubscriptionKey cannot be null or empty.");
            }
        }

        private void InitializeHttpClient()
        {
            _httpClient = new HttpClient
            {
                MaxResponseContentBufferSize = Int32.MaxValue,
                Timeout = TimeSpan.FromSeconds(30)
            };

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this._ocpApimSubscriptionKey);
        }
    }
}
