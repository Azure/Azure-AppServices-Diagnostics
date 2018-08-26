using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.RuntimeHost.Models.QnAModels;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services
{
    public class QnAService : IQnAService
    {
        private IHostingEnvironment _env;
        private readonly IConfiguration _config;
        private ConcurrentQueue<QnAServiceRequest> _requestQueue;
        private QnAMakerClient _qnaMakerClient;
        private bool _qnaPipelineEnabled;
        private int _qnaPipelineTriggerIntervalInSeconds;
        
        public QnAService(IHostingEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _config = configuration;

            _qnaMakerClient = new QnAMakerClient(_env, _config);
            _requestQueue = new ConcurrentQueue<QnAServiceRequest>();

            LoadConfigurations();

            if (_qnaPipelineEnabled)
            {
                StartQnAPipeline();
            }
        }

        public void EnqueueQnAServiceRequest(QnAServiceRequest request)
        {
            if(request == null || !_qnaPipelineEnabled)
            {
                return;
            }

            _requestQueue.Enqueue(request);
        }

        private async Task StartQnAPipeline()
        {
            do
            {
                await Task.Delay(_qnaPipelineTriggerIntervalInSeconds * 1000);
                await ProcessQnAServiceRequests();

            } while (true);
        }

        private async Task ProcessQnAServiceRequests()
        {
            var requestsInQueue = GetRequestQueueSnapshot();
            if(requestsInQueue == null || !requestsInQueue.Any())
            {
                return;
            }

            var requestsPerResourceType = requestsInQueue.GroupBy(p => p.QnAResourceType);
            List<Task> resourceTasks = new List<Task>();

            foreach (var item in requestsPerResourceType)
            {
                resourceTasks.Add(ProcessQnAServiceRequestsPerResource(item.Key, item.ToList()));
            }

            await Task.WhenAll(resourceTasks);
        }

        private async Task ProcessQnAServiceRequestsPerResource(QnAResourceType resourceType, List<QnAServiceRequest> serviceRequests)
        {
            if(serviceRequests == null || !serviceRequests.Any())
            {
                return;
            }

            string kbId = GetKBIdForResourceType(resourceType);
            if (string.IsNullOrWhiteSpace(kbId))
            {
                return;
            }

            var existingQnAPairs = await this._qnaMakerClient.GetAllQnAPairsInKB(kbId);

            List<QnAPair> pairsToBeAdded = new List<QnAPair>();
            List<QnAPair> pairsToBeDeleted = new List<QnAPair>();
            List<QnAPairUpdateModel> pairsToBeUpdated = new List<QnAPairUpdateModel>();

            foreach (var request in serviceRequests)
            {
                QnAPair existingPair = null;

                switch (request.OperationType)
                {
                    case QnAOperationType.Delete:

                        existingPair = FindExistingQnAPairForDetector(existingQnAPairs, request.DetectorDefinition);
                        if (existingPair != null)
                        {
                            pairsToBeDeleted.Add(existingPair);
                        }

                        break;

                    case QnAOperationType.Add:

                        existingPair = FindExistingQnAPairForDetector(existingQnAPairs, request.DetectorDefinition);

                        QnAPair newPair = new QnAPair()
                        {
                            Answer = $"{JsonConvert.SerializeObject(request.DetectorDefinition)}",
                            Questions = new List<string>()
                            {
                                request.DetectorDefinition.Name
                            }
                        };

                        if (existingPair == null)
                        {
                            /* First Scenario
                             * If the detector doesn't exist in QnA KB, then only add the detector if its Public.
                             * Do Not add private detectors for now.
                             */

                            if (request.IsDetectorPublic)
                            {
                                pairsToBeAdded.Add(newPair);
                            }
                        }
                        else
                        {
                            Definition existingDefiniton = JsonConvert.DeserializeObject<Definition>(existingPair.Answer);

                            /* Second Scenario
                             * If the detector exists in QnA KB, and there was a update in the Name of the detector
                             * then tag this as an update request.
                             */

                            if (request.IsDetectorPublic && !request.DetectorDefinition.Name.Equals(existingDefiniton.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                pairsToBeUpdated.Add(new QnAPairUpdateModel()
                                {
                                    Id = existingPair.Id,
                                    Answer = existingPair.Answer,
                                    Questions = new QnaPairUpdateQuestionModel(new List<string>() { request.DetectorDefinition.Name }, new List<string>() { existingDefiniton.Name })
                                });
                            }
                            else if (!request.IsDetectorPublic)
                            {
                                /* Third Scenario
                                 * If the detector is marked private and it exists in QnA KB, 
                                 * then tag it for deletion.
                                 */

                                pairsToBeDeleted.Add(existingPair);
                            }
                        }

                        break;
                }
            }

            await this._qnaMakerClient.UpdateAndTrainKB(kbId, pairsToBeAdded, pairsToBeDeleted, pairsToBeUpdated);
            await this._qnaMakerClient.PublishKB(kbId);
        }

        private QnAPair FindExistingQnAPairForDetector(List<QnAPair> existingQnAPairs, Definition detectorDefinition)
        {
            return existingQnAPairs.FirstOrDefault(p =>
            JsonConvert.DeserializeObject<Definition>(p.Answer).Id.Equals(detectorDefinition.Id, StringComparison.OrdinalIgnoreCase));
        }

        private List<QnAServiceRequest> GetRequestQueueSnapshot()
        {
            var queueSnapshot = new List<QnAServiceRequest>();
            int currentRequestQueueCapacity = _requestQueue.Count;

            for (int iterator = 0; iterator < currentRequestQueueCapacity; iterator++)
            {
                QnAServiceRequest queueItem;
                if (_requestQueue.TryDequeue(out queueItem))
                {
                    queueSnapshot.Add(queueItem);
                }
            }

            return queueSnapshot;
        }

        private string GetKBIdForResourceType(QnAResourceType resourceType)
        {
            switch (resourceType)
            {
                case QnAResourceType.WebAppWindows:
                    return "08559957-57d4-45e5-9aa5-6b2e25ebbc9c";
                case QnAResourceType.WebAppLinux:
                    return "0eb89862-eb8a-4e39-ba1f-0232e36bfefe";
                case QnAResourceType.FunctionApp:
                    return "3c9775bb-f347-4ae4-a38b-6d95b9b6695f";
                case QnAResourceType.ASE:
                    return "e3580903-acd3-4bed-976b-93de7baa96ba";
                case QnAResourceType.LogicApp:
                    return "7e517efd-3dee-47be-bb67-af3610c4c576";
                default:
                    return string.Empty;
            }
        }

        private void LoadConfigurations()
        {
            string qnaEnabledString = "false";
            if (_env.IsProduction())
            {
                qnaEnabledString = (string)Registry.GetValue(RegistryConstants.QnAKnowledgeBaseRegistryPath, RegistryConstants.QnAPipelineEnabledKey, "false");
                _qnaPipelineTriggerIntervalInSeconds = Convert.ToInt32((string)Registry.GetValue(RegistryConstants.QnAKnowledgeBaseRegistryPath, RegistryConstants.QnAPipelineTriggerIntervalInSecondsKey, "60"));
            }
            else
            {
                qnaEnabledString = (_config[$"QnAKnowledgeBase:{RegistryConstants.QnAPipelineEnabledKey}"]).ToString();
                _qnaPipelineTriggerIntervalInSeconds = Convert.ToInt32((_config[$"QnAKnowledgeBase:{RegistryConstants.QnAPipelineTriggerIntervalInSecondsKey}"]).ToString());
            }

            if (!bool.TryParse(qnaEnabledString, out _qnaPipelineEnabled))
            {
                _qnaPipelineEnabled = false;
            }
        }
    }
}
