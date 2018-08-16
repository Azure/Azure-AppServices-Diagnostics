using Diagnostics.DataProviders;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.GeoMaster;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Controllers
{
    public abstract class DiagnosticControllerBase<TResource> : Controller where TResource : IResource
    {
        protected ICompilerHostClient _compilerHostClient;
        protected ISourceWatcherService _sourceWatcherService;
        protected IInvokerCacheService _invokerCache;
        protected IDataSourcesConfigurationService _dataSourcesConfigService;
        protected IStampService _stampService;

        public DiagnosticControllerBase(IStampService stampService, ICompilerHostClient compilerHostClient, ISourceWatcherService sourceWatcherService, IInvokerCacheService invokerCache, IDataSourcesConfigurationService dataSourcesConfigService)
        {
            this._compilerHostClient = compilerHostClient;
            this._sourceWatcherService = sourceWatcherService;
            this._invokerCache = invokerCache;
            this._dataSourcesConfigService = dataSourcesConfigService;
            this._stampService = stampService;
        }

        #region API Response Methods

        protected async Task<IActionResult> ListDetectors(TResource resource)
        {
            DateTimeHelper.PrepareStartEndTimeWithTimeGrain(string.Empty, string.Empty, string.Empty, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage);
            OperationContext<TResource> cxt = PrepareContext(resource, startTimeUtc, endTimeUtc);
            return Ok(await this.ListDetectorsInternal(cxt));
        }

        protected async Task<IActionResult> GetDetector(TResource resource, string detectorId, string startTime, string endTime, string timeGrain)
        {
            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            OperationContext<TResource> cxt = PrepareContext(resource, startTimeUtc, endTimeUtc);
            var detectorResponse = await GetDetectorInternal(detectorId, cxt);
            return detectorResponse == null ? (IActionResult)NotFound() : Ok(DiagnosticApiResponse.FromCsxResponse(detectorResponse.Item1, detectorResponse.Item2));
        }

        protected async Task<IActionResult> ListSystemInvokers(TResource resource)
        {
            DateTimeHelper.PrepareStartEndTimeWithTimeGrain(string.Empty, string.Empty, string.Empty, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage);
            OperationContext<TResource> context = PrepareContext(resource, startTimeUtc, endTimeUtc);

            await this._sourceWatcherService.Watcher.WaitForFirstCompletion();

            var systemInvokers = _invokerCache.GetSystemInvokerList<TResource>(context)
               .Select(p => new DiagnosticApiResponse { Metadata = p.EntryPointDefinitionAttribute });

            return Ok(systemInvokers);
        }
        
        protected async Task<IActionResult> GetSystemInvoker(TResource resource, string detectorId, string invokerId, string dataSource, string timeRange)
        {
            Dictionary<string, dynamic> systemContext = PrepareSystemContext(resource, detectorId, dataSource, timeRange);

            await this._sourceWatcherService.Watcher.WaitForFirstCompletion();
            var dataProviders = new DataProviders.DataProviders(_dataSourcesConfigService.Config);
            var invoker = this._invokerCache.GetSystemInvoker(invokerId);

            if (invoker == null)
            {
                return null;
            }

            Response res = new Response
            {
                Metadata = invoker.EntryPointDefinitionAttribute
            };

            var response = (Response)await invoker.Invoke(new object[] { dataProviders, systemContext, res });

            List<DataProviderMetadata> dataProvidersMetadata = GetDataProvidersMetadata(dataProviders);

            return response == null ? (IActionResult)NotFound() : Ok(DiagnosticApiResponse.FromCsxResponse(response, dataProvidersMetadata));
        }

        protected async Task<IActionResult> ExecuteQuery<TPostBodyResource>(TResource resource, CompilationBostBody<TPostBodyResource> jsonBody, string startTime, string endTime, string timeGrain, string detectorId = null, string dataSource = null, string timeRange = null)
        {
            if (jsonBody == null)
            {
                return BadRequest("Missing body");
            }

            if (string.IsNullOrWhiteSpace(jsonBody.Script))
            {
                return BadRequest("Missing script in body");
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            await this._sourceWatcherService.Watcher.WaitForFirstCompletion();
            EntityMetadata metaData = new EntityMetadata(jsonBody.Script);
            var dataProviders = new DataProviders.DataProviders(_dataSourcesConfigService.Config);

            QueryResponse<DiagnosticApiResponse> queryRes = new QueryResponse<DiagnosticApiResponse>
            {
                InvocationOutput = new DiagnosticApiResponse()
            };

            Assembly tempAsm = null;
            this.Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues requestIds);
            var compilerResponse = await _compilerHostClient.GetCompilationResponse(jsonBody.Script, requestIds.FirstOrDefault() ?? string.Empty);
            queryRes.CompilationOutput = compilerResponse;

            if (queryRes.CompilationOutput.CompilationSucceeded)
            {
                byte[] asmData = Convert.FromBase64String(compilerResponse.AssemblyBytes);
                byte[] pdbData = Convert.FromBase64String(compilerResponse.PdbBytes);

                tempAsm = Assembly.Load(asmData, pdbData);

                using (var invoker = new EntityInvoker(metaData, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
                {
                    invoker.InitializeEntryPoint(tempAsm);

                    // Verify Detector with other detectors in the system in case of conflicts
                    List<DataProviderMetadata> dataProvidersMetadata = null;
                    Response invocationResponse = null;
                    bool isInternalCall = true;
                    try
                    {
                        if (detectorId == null)
                        {
                            if (!VerifyEntity(invoker, ref queryRes)) return Ok(queryRes);
                            OperationContext<TResource> cxt = PrepareContext(resource, startTimeUtc, endTimeUtc);

                            var responseInput = new Response() { Metadata = RemovePIIFromDefinition(invoker.EntryPointDefinitionAttribute, cxt.IsInternalCall) };
                            invocationResponse = (Response)await invoker.Invoke(new object[] { dataProviders, cxt, responseInput });
                            invocationResponse.UpdateDetectorStatusFromInsights();
                            isInternalCall = cxt.IsInternalCall;
                        }
                        else
                        {
                            Dictionary<string, dynamic> systemContext = PrepareSystemContext(resource, detectorId, dataSource, timeRange);
                            var responseInput = new Response() { Metadata = invoker.EntryPointDefinitionAttribute };
                            invocationResponse = (Response)await invoker.Invoke(new object[] { dataProviders, systemContext, responseInput });
                        }

                        if (isInternalCall)
                        {
                            dataProvidersMetadata = GetDataProvidersMetadata(dataProviders);
                        }

                        queryRes.RuntimeSucceeded = true;
                        queryRes.InvocationOutput = DiagnosticApiResponse.FromCsxResponse(invocationResponse, dataProvidersMetadata);
                    }
                    catch (Exception ex)
                    {
                        if (isInternalCall)
                        {
                            queryRes.RuntimeSucceeded = false;
                            queryRes.InvocationOutput = CreateQueryExceptionResponse(ex, invoker.EntryPointDefinitionAttribute, isInternalCall, GetDataProvidersMetadata(dataProviders));
                        }
                        else
                            throw;
                    }

                }
            }

            return Ok(queryRes);
        }

        protected async Task<IActionResult> PublishDetector(DetectorPackage pkg)
        {
            if (pkg == null || string.IsNullOrWhiteSpace(pkg.Id) || string.IsNullOrWhiteSpace(pkg.DllBytes))
            {
                return BadRequest();
            }

            var publishResult = await _sourceWatcherService.Watcher.CreateOrUpdateDetector(pkg);
            bool isPublishSuccessful = publishResult.Item1;
            Exception publishEx = publishResult.Item2;

            if (!isPublishSuccessful)
            {
                if(publishEx != null)
                {
                    throw publishEx;
                }

                throw new Exception("Publish Operation failed");
            }

            return Ok();
        }

        private DiagnosticApiResponse CreateQueryExceptionResponse(Exception ex, Definition detectorDefinition, bool isInternal, List<DataProviderMetadata> dataProvidersMetadata)
        {
            Response response = new Response() { Metadata = RemovePIIFromDefinition(detectorDefinition, isInternal) };
            response.AddMarkdownView($"<pre><code>{ex.ToString()}</code></pre>", "Detector Runtime Exception");
            return DiagnosticApiResponse.FromCsxResponse(response, dataProvidersMetadata);
        }

        protected async Task<IActionResult> GetInsights(TResource resource, string supportTopicId, string minimumSeverity, string startTime, string endTime, string timeGrain)
        {
            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            OperationContext<TResource> cxt = PrepareContext(resource, startTimeUtc, endTimeUtc, forceInternal: true);

            List<AzureSupportCenterInsight> insights = null;
            string error = null;
            List<Definition> detectorsRun = new List<Definition>();
            try
            {
                supportTopicId = ParseCorrectSupportTopicId(supportTopicId);
                var allDetectors = (await ListDetectorsInternal(cxt)).Select(detectorResponse => detectorResponse.Metadata);

                var applicableDetectors = allDetectors
                    .Where(detector => string.IsNullOrWhiteSpace(supportTopicId) || detector.SupportTopicList.FirstOrDefault(supportTopic => supportTopic.Id == supportTopicId) != null);

                var insightGroups = await Task.WhenAll(applicableDetectors.Select(detector => GetInsightsFromDetector(cxt, detector, detectorsRun)));

                insights = insightGroups.Where(group => group != null).SelectMany(group => group).ToList();
            }
            catch (Exception ex)
            {
                error = ex.GetType().ToString();
                DiagnosticsETWProvider.Instance.LogRuntimeHostHandledException(cxt.RequestId, "GetInsights", cxt.Resource.SubscriptionId, cxt.Resource.ResourceGroup,
                    cxt.Resource.Name, ex.GetType().ToString(), ex.ToString());
            }


            var correlationId = Guid.NewGuid();
            var insightInfo = new
            {
                Total = insights.Count,
                Critical = insights.Count(insight => insight.ImportanceLevel == ImportanceLevel.Critical),
                Warning = insights.Count(insight => insight.ImportanceLevel == ImportanceLevel.Warning),
                Info = insights.Count(insight => insight.ImportanceLevel == ImportanceLevel.Info),
                Default = detectorsRun.Any() && !insights.Any() ? 1 : 0
            };

            DiagnosticsETWProvider.Instance.LogRuntimeHostInsightCorrelation(cxt.RequestId, "ControllerBase.GetInsights", cxt.Resource.SubscriptionId,
                cxt.Resource.ResourceGroup, cxt.Resource.Name, correlationId.ToString(), JsonConvert.SerializeObject(insightInfo));

            if (!insights.Any() && detectorsRun.Any())
            {
                insights.Add(AzureSupportCenterInsightUtilites.CreateDefaultInsight(cxt, detectorsRun));
            }

            var response = new AzureSupportCenterInsightEnvelope()
            {
                CorrelationId = correlationId,
                ErrorMessage = error,
                TotalInsightsFound = insights != null ? insights.Count() : 0,
                Insights = insights
            };

            return Ok(response);
        }

        #endregion

        protected TResource GetResource(string subscriptionId, string resourceGroup, string name)
        {
            return (TResource)Activator.CreateInstance(typeof(TResource), subscriptionId, resourceGroup, name);
        }

        // Purposefully leaving this method in Base class. This method is shared between two resources right now - HostingEnvironment and WebApp
        protected async Task<HostingEnvironment> GetHostingEnvironment(string subscriptionId, string resourceGroup, string name, DiagnosticStampData stampPostBody, DateTime startTime, DateTime endTime)
        {
            if (stampPostBody == null)
            {
                return new HostingEnvironment(subscriptionId, resourceGroup, name);
            }

            string requestId = string.Empty;
            if (this.Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues requestIds))
            {
                requestId = requestIds.FirstOrDefault() ?? string.Empty;
            }

            HostingEnvironment hostingEnv = new HostingEnvironment(subscriptionId, resourceGroup, name)
            {
                Name = stampPostBody.InternalName,
                InternalName = stampPostBody.InternalName,
                ServiceAddress = stampPostBody.ServiceAddress,
                State = stampPostBody.State,
                DnsSuffix = stampPostBody.DnsSuffix,
                UnhealthySince = stampPostBody.UnhealthySince,
                SuspendedOn = stampPostBody.SuspendedOn,
                Location = stampPostBody.Location
            };

            switch (stampPostBody.Kind)
            {
                case DiagnosticStampType.ASEV1:
                    hostingEnv.HostingEnvironmentType = HostingEnvironmentType.V1;
                    break;
                case DiagnosticStampType.ASEV2:
                    hostingEnv.HostingEnvironmentType = HostingEnvironmentType.V2;
                    break;
                default:
                    hostingEnv.HostingEnvironmentType = HostingEnvironmentType.None;
                    break;
            }

            string stampName = !string.IsNullOrWhiteSpace(hostingEnv.InternalName) ? hostingEnv.InternalName : hostingEnv.Name;

            var result = await this._stampService.GetTenantIdForStamp(stampName, startTime, endTime, requestId);
            hostingEnv.TenantIdList = result.Item1;
            hostingEnv.PlatformType = result.Item2;

            return hostingEnv;
        }

        private Dictionary<string, dynamic> PrepareSystemContext(TResource resource, string detectorId, string dataSource, string timeRange)
        {
            dataSource = string.IsNullOrWhiteSpace(dataSource) ? "0" : dataSource;
            timeRange = string.IsNullOrWhiteSpace(timeRange) ? "168" : timeRange;

            this.Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues requestIds);
            this.Request.Headers.TryGetValue(HeaderConstants.InternalCallHeaderName, out StringValues internalCallHeader);
            bool isInternalRequest = false;
            if (internalCallHeader.Any())
            {
                bool.TryParse(internalCallHeader.First(), out isInternalRequest);
            }

            OperationContext<TResource> cxt = new OperationContext<TResource>(
                resource,
                "",
                "",
                isInternalRequest,
                requestIds.FirstOrDefault()
            );

            var invoker = this._invokerCache.GetDetectorInvoker<TResource>(detectorId, cxt);
            IEnumerable<SupportTopic> supportTopicList = null;
            if (invoker != null && invoker.EntryPointDefinitionAttribute != null && invoker.EntryPointDefinitionAttribute.SupportTopicList != null && invoker.EntryPointDefinitionAttribute.SupportTopicList.Any())
            {
                supportTopicList = invoker.EntryPointDefinitionAttribute.SupportTopicList;
            }
                
            Dictionary<string, dynamic> systemContext = new Dictionary<string, dynamic>();
            systemContext.Add("detectorId", detectorId);
            systemContext.Add("requestIds", requestIds);
            systemContext.Add("isInternal", isInternalRequest);
            systemContext.Add("dataSource", dataSource);
            systemContext.Add("timeRange", timeRange);
            systemContext.Add("supportTopicList", supportTopicList);
            return systemContext;
        }
        private OperationContext<TResource> PrepareContext(TResource resource, DateTime startTime, DateTime endTime, bool forceInternal = false)
        {
            this.Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues requestIds);
            this.Request.Headers.TryGetValue(HeaderConstants.InternalCallHeaderName, out StringValues internalCallHeader);
            bool isInternalRequest = false;
            if (internalCallHeader.Any())
            {
                bool.TryParse(internalCallHeader.First(), out isInternalRequest);
            }

            return new OperationContext<TResource>(
                resource,
                DateTimeHelper.GetDateTimeInUtcFormat(startTime).ToString(DataProviderConstants.KustoTimeFormat),
                DateTimeHelper.GetDateTimeInUtcFormat(endTime).ToString(DataProviderConstants.KustoTimeFormat),
                isInternalRequest || forceInternal,
                requestIds.FirstOrDefault()
            );
        }

        private async Task<IEnumerable<DiagnosticApiResponse>> ListDetectorsInternal(OperationContext<TResource> context)
        {
            await this._sourceWatcherService.Watcher.WaitForFirstCompletion();

            return _invokerCache.GetDetectorInvokerList<TResource>(context)
                .Select(p => new DiagnosticApiResponse { Metadata = RemovePIIFromDefinition(p.EntryPointDefinitionAttribute, context.IsInternalCall) });
        }

        private async Task<Tuple<Response, List<DataProviderMetadata>>> GetDetectorInternal(string detectorId, OperationContext<TResource> context)
        {
            await this._sourceWatcherService.Watcher.WaitForFirstCompletion();
            var dataProviders = new DataProviders.DataProviders(_dataSourcesConfigService.Config);
            var invoker = this._invokerCache.GetDetectorInvoker<TResource>(detectorId, context);

            if (invoker == null)
            {
                return null;
            }

            Response res = new Response
            {
                Metadata = RemovePIIFromDefinition(invoker.EntryPointDefinitionAttribute, context.IsInternalCall)
            };

            var response = (Response)await invoker.Invoke(new object[] { dataProviders, context, res });

            List<DataProviderMetadata> dataProvidersMetadata = null;
            response.UpdateDetectorStatusFromInsights();

            if (context.IsInternalCall)
            {
                dataProvidersMetadata = GetDataProvidersMetadata(dataProviders);
            }

            return new Tuple<Response, List<DataProviderMetadata>>(response, dataProvidersMetadata);
        }

        private async Task<IEnumerable<AzureSupportCenterInsight>> GetInsightsFromDetector(OperationContext<TResource> context, Definition detector, List<Definition> detectorsRun)
        {
            Response response = null;

            detectorsRun.Add(detector);

            try
            {
                var fullResponse = await GetDetectorInternal(detector.Id, context);
                if (fullResponse != null)
                {
                    response = fullResponse.Item1;
                }
            }
            catch (Exception ex)
            {
                DiagnosticsETWProvider.Instance.LogRuntimeHostHandledException(context.RequestId, "GetInsightsFromDetector", context.Resource.SubscriptionId,
                    context.Resource.ResourceGroup, context.Resource.Name, ex.GetType().ToString(), ex.ToString());
            }

            // Handle Exception or Not Found
            // Not found can occur if invalid detector is put in detector list
            if (response == null)
            {
                return null;
            }

            List<AzureSupportCenterInsight> supportCenterInsights = new List<AzureSupportCenterInsight>();

            // Take max one insight per detector, only critical or warning, pick the most critical
            var mostCriticalInsight = response.Insights.OrderBy(insight => insight.Status).FirstOrDefault();

            //TODO: Add Logging Per Detector Here
            AzureSupportCenterInsight ascInsight = null;
            if (mostCriticalInsight != null)
            {
                ascInsight = AzureSupportCenterInsightUtilites.CreateInsight(mostCriticalInsight, context, detector);
                supportCenterInsights.Add(ascInsight);
            }

            DiagnosticsETWProvider.Instance.LogRuntimeHostDetectorAscInsight(context.RequestId, detector.Id, ascInsight?.ImportanceLevel.ToString());

            var detectorLists = response.Dataset
                .Where(diagnosicData => diagnosicData.RenderingProperties.Type == RenderingType.Detector)
                .SelectMany(diagnosticData => ((DetectorCollectionRendering)diagnosticData.RenderingProperties).DetectorIds)
                .Distinct();

            if (detectorLists.Any())
            {
                var applicableDetectorMetaData = (await this.ListDetectorsInternal(context)).Where(detectorResponse => detectorLists.Contains(detectorResponse.Metadata.Id));
                var detectorListResponses = await Task.WhenAll(applicableDetectorMetaData.Select(detectorResponse => GetInsightsFromDetector(context, detectorResponse.Metadata, detectorsRun)));

                supportCenterInsights.AddRange(detectorListResponses.Where(detectorInsights => detectorInsights != null).SelectMany(detectorInsights => detectorInsights));
            }

            return supportCenterInsights;
        }

        // The reason we have this method is that Azure Support Center will pass support topic id in the format below:
        // 1003023/32440119/32457411 
        // But the support topic we are using is only the last one
        private string ParseCorrectSupportTopicId(string supportTopicId)
        {
            if (supportTopicId == null)
            {
                return null;
            }

            string[] subIds = supportTopicId.Split("\\");
            return subIds[subIds.Length - 1];
        }

        private List<DataProviderMetadata> GetDataProvidersMetadata(DataProviders.DataProviders dataProviders)
        {
            var dataprovidersMetadata = new List<DataProviderMetadata>();
            foreach (var dataProvider in dataProviders.GetType().GetFields())
            {
                if (dataProvider.FieldType.IsInterface)
                {
                    var metadataProvider = dataProvider.GetValue(dataProviders) as IMetadataProvider;
                    var metadata = metadataProvider.GetMetadata();
                    if (metadata != null)
                    {
                        dataprovidersMetadata.Add(metadata);
                    }
                }
            }
            return dataprovidersMetadata;
        }

        private bool VerifyEntity(EntityInvoker invoker, ref QueryResponse<DiagnosticApiResponse> queryRes)
        {
            List<EntityInvoker> allDetectors = this._invokerCache.GetAll().ToList();

            foreach (var topicId in invoker.EntryPointDefinitionAttribute.SupportTopicList)
            {
                var existingDetector = allDetectors.FirstOrDefault(p =>
                (!p.EntryPointDefinitionAttribute.Id.Equals(invoker.EntryPointDefinitionAttribute.Id, StringComparison.OrdinalIgnoreCase) && p.EntryPointDefinitionAttribute.SupportTopicList.Contains(topicId)));
                if (existingDetector != default(EntityInvoker))
                {
                    // There exists a detector which has same support topic id.
                    queryRes.CompilationOutput.CompilationSucceeded = false;
                    queryRes.CompilationOutput.AssemblyBytes = string.Empty;
                    queryRes.CompilationOutput.PdbBytes = string.Empty;
                    queryRes.CompilationOutput.CompilationOutput = queryRes.CompilationOutput.CompilationOutput.Concat(new List<string>()
                    {
                        $"Error : There is already a detector(id : {existingDetector.EntryPointDefinitionAttribute.Id}, name : {existingDetector.EntryPointDefinitionAttribute.Name})" +
                        $" that uses the SupportTopic (id : {topicId.Id}, pesId : {topicId.PesId}). System can't have two detectors for same support topic id. Consider merging these two detectors."
                    });

                    return false;
                }
            }

            return true;
        }

        private Definition RemovePIIFromDefinition(Definition definition, bool isInternal)
        {
            if (!isInternal) definition.Author = string.Empty;
            return definition;
        }
    }
}
