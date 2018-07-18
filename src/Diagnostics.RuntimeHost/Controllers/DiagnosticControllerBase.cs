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

        protected async Task<IActionResult> ExecuteQuery<TPostBodyResource>(TResource resource, CompilationBostBody<TPostBodyResource> jsonBody, string startTime, string endTime, string timeGrain)
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
                    if (!VerifyEntity(invoker, ref queryRes)) return Ok(queryRes);
                    OperationContext<TResource> cxt = PrepareContext(resource, startTimeUtc, endTimeUtc);
                    List<DataProviderMetadata> dataProvidersMetadata = null;

                    try
                    {
                        var responseInput = new Response() { Metadata = RemovePIIFromDefinition(invoker.EntryPointDefinitionAttribute, cxt.IsInternalCall) };
                        var invocationResponse = (Response)await invoker.Invoke(new object[] { dataProviders, cxt, responseInput });
                        invocationResponse.UpdateDetectorStatusFromInsights();

                        if (cxt.IsInternalCall)
                        {
                            dataProvidersMetadata = GetDataProvidersMetadata(dataProviders);
                        }

                        queryRes.RuntimeSucceeded = true;
                        queryRes.InvocationOutput = DiagnosticApiResponse.FromCsxResponse(invocationResponse, dataProvidersMetadata);
                    }
                    catch(Exception ex)
                    {
                        if (cxt.IsInternalCall)
                        {
                            queryRes.RuntimeSucceeded = false;
                            queryRes.InvocationOutput = CreateQueryExceptionResponse(ex, invoker.EntryPointDefinitionAttribute, cxt.IsInternalCall, GetDataProvidersMetadata(dataProviders));
                        }
                        else
                            throw;
                    }
                    
                }
            }

            return Ok(queryRes);
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

            IEnumerable<AzureSupportCenterInsight> insights = null;
            string error = null;
            try
            {
                supportTopicId = ParseCorrectSupportTopicId(supportTopicId);
                var allDetectors = (await ListDetectorsInternal(cxt)).Select(detectorResponse => detectorResponse.Metadata);

                var applicableDetectors = allDetectors
                    .Where(detector => string.IsNullOrWhiteSpace(supportTopicId) || detector.SupportTopicList.FirstOrDefault(supportTopic => supportTopic.Id == supportTopicId) != null);

                var insightGroups = await Task.WhenAll(applicableDetectors.Select(detector => GetInsightsFromDetector(cxt, detector)));

                insights = insightGroups.Where(group => group != null).SelectMany(group => group);
            }
            catch (Exception ex)
            {
                error = ex.GetType().ToString();
                DiagnosticsETWProvider.Instance.LogRuntimeHostHandledException(cxt.RequestId, "GetInsights", cxt.Resource.SubscriptionId, cxt.Resource.ResourceGroup,
                    cxt.Resource.Name, ex.GetType().ToString(), ex.ToString());
            }


            var correlationId = Guid.NewGuid();
            DiagnosticsETWProvider.Instance.LogRuntimeHostInsightCorrelation(cxt.RequestId, "ControllerBase.GetInsights", cxt.Resource.SubscriptionId,
                cxt.Resource.ResourceGroup, cxt.Resource.Name, correlationId.ToString());

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

            return _invokerCache.GetInvokerList<TResource>(context)
                .Select(p => new DiagnosticApiResponse { Metadata = RemovePIIFromDefinition(p.EntryPointDefinitionAttribute, context.IsInternalCall) });
        }
        
        private async Task<Tuple<Response, List<DataProviderMetadata>>> GetDetectorInternal(string detectorId, OperationContext<TResource> context)
        {
            await this._sourceWatcherService.Watcher.WaitForFirstCompletion();
            var dataProviders = new DataProviders.DataProviders(_dataSourcesConfigService.Config);
            var invoker = this._invokerCache.GetInvoker<TResource>(detectorId, context);

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

            return new Tuple<Response, List<DataProviderMetadata>>(response, dataProvidersMetadata) ;
        }
        
        private async Task<IEnumerable<AzureSupportCenterInsight>> GetInsightsFromDetector(OperationContext<TResource> context, Definition detector)
        {
            Response response = null;
            try
            {
                response = (await GetDetectorInternal(detector.Id, context)).Item1;
            }
            catch(Exception ex)
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
            var mostCriticalInsight = response.Insights.Where(insight => ((int)insight.Status) <= ((int)InsightStatus.Info)).OrderBy(insight => insight.Status).FirstOrDefault();

            if (mostCriticalInsight != null)
            {
                supportCenterInsights.Add(AzureSupportCenterInsightUtilites.CreateInsight(mostCriticalInsight, context, detector));
            }

            var detectorLists = response.Dataset
                .Where(diagnosicData => diagnosicData.RenderingProperties.Type == RenderingType.Detector)
                .SelectMany(diagnosticData => ((DetectorCollectionRendering)diagnosticData.RenderingProperties).DetectorIds)
                .Distinct();

            if (detectorLists.Any())
            {
                var applicableDetectorMetaData = (await this.ListDetectorsInternal(context)).Where(detectorResponse => detectorLists.Contains(detectorResponse.Metadata.Id));
                var detectorListResponses = await Task.WhenAll(applicableDetectorMetaData.Select(detectorResponse => GetInsightsFromDetector(context, detectorResponse.Metadata)));

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
