using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using Diagnostics.DataProviders;
using Diagnostics.DataProviders.Exceptions;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using Diagnostics.ModelsAndUtils.Utilities;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Models.Exceptions;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Diagnostics.Scripts.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Authorize]
    public abstract class DiagnosticControllerBase<TResource> : Controller where TResource : IResource
    {
        protected ICompilerHostClient _compilerHostClient;
        protected ISourceWatcherService _sourceWatcherService;
        protected IInvokerCacheService _invokerCache;
        protected IGistCacheService _gistCache;
        protected IDataSourcesConfigurationService _dataSourcesConfigService;
        protected IStampService _stampService;
        protected IAssemblyCacheService _assemblyCacheService;
        protected ISearchService _searchService;
        protected IRuntimeContext<TResource> _runtimeContext;
        protected ISupportTopicService _supportTopicService;
        protected IKustoMappingsCacheService _kustoMappingCacheService;
        protected IRuntimeLoggerProvider _loggerProvider;

        private InternalAPIHelper _internalApiHelper;

        public DiagnosticControllerBase(IServiceProvider services, IRuntimeContext<TResource> runtimeContext)
        {
            this._compilerHostClient = (ICompilerHostClient)services.GetService(typeof(ICompilerHostClient));
            this._sourceWatcherService = (ISourceWatcherService)services.GetService(typeof(ISourceWatcherService));
            this._invokerCache = (IInvokerCacheService)services.GetService(typeof(IInvokerCacheService));
            this._gistCache = (IGistCacheService)services.GetService(typeof(IGistCacheService));
            this._dataSourcesConfigService = (IDataSourcesConfigurationService)services.GetService(typeof(IDataSourcesConfigurationService));
            this._stampService = (IStampService)services.GetService(typeof(IStampService));
            this._assemblyCacheService = (IAssemblyCacheService)services.GetService(typeof(IAssemblyCacheService));
            this._searchService = (ISearchService)services.GetService(typeof(ISearchService));
            this._supportTopicService = (ISupportTopicService)services.GetService(typeof(ISupportTopicService));
            this._kustoMappingCacheService = (IKustoMappingsCacheService)services.GetService(typeof(IKustoMappingsCacheService));
            this._loggerProvider = (IRuntimeLoggerProvider)services.GetService(typeof(IRuntimeLoggerProvider));

            this._internalApiHelper = new InternalAPIHelper();
            _runtimeContext = runtimeContext;
        }

        #region API Response Methods

        protected async Task<IActionResult> ListDetectors(TResource resource, string queryText = null)
        {
            DateTimeHelper.PrepareStartEndTimeWithTimeGrain(string.Empty, string.Empty, string.Empty, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage);
            RuntimeContext<TResource> cxt = PrepareContext(resource, startTimeUtc, endTimeUtc);
            queryText = HttpUtility.UrlDecode(queryText);
            if (queryText != null && queryText.Length < 2)
            {
                return BadRequest("Search query term should be at least two characters");
            }
            return Ok(await this.ListDetectorsInternal(cxt, queryText));
        }

        protected async Task<IActionResult> GetDetector(TResource resource, string detectorId, string startTime, string endTime, string timeGrain, Form form = null)
        {
            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            RuntimeContext<TResource> cxt = PrepareContext(resource, startTimeUtc, endTimeUtc, Form: form, detectorId: detectorId);
            var detectorResponse = await GetDetectorInternal(detectorId, cxt);
            return detectorResponse == null ? (IActionResult)NotFound() : Ok(DiagnosticApiResponse.FromCsxResponse(detectorResponse.Item1, detectorResponse.Item2));
        }

        protected async Task<IActionResult> ListGists(TResource resource)
        {
            DateTimeHelper.PrepareStartEndTimeWithTimeGrain(string.Empty, string.Empty, string.Empty, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage);
            RuntimeContext<TResource> cxt = PrepareContext(resource, startTimeUtc, endTimeUtc);
            return Ok(await ListGistsInternal(cxt));
        }

        protected async Task<IActionResult> GetGist(TResource resource, string id, string startTime, string endTime, string timeGrain)
        {
            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            RuntimeContext<TResource> cxt = PrepareContext(resource, startTimeUtc, endTimeUtc);
            return Ok(await GetGistInternal(id, cxt));
        }

        protected async Task<IActionResult> ListSystemInvokers(TResource resource)
        {
            DateTimeHelper.PrepareStartEndTimeWithTimeGrain(string.Empty, string.Empty, string.Empty, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage);
            RuntimeContext<TResource> context = PrepareContext(resource, startTimeUtc, endTimeUtc);

            await this._sourceWatcherService.Watcher.WaitForFirstCompletion();

            var systemInvokers = _invokerCache.GetSystemInvokerList<TResource>(context)
               .Select(p => new DiagnosticApiResponse { Metadata = p.EntryPointDefinitionAttribute });

            return Ok(systemInvokers);
        }

        protected async Task<IActionResult> GetSystemInvoker(TResource resource, string detectorId, string invokerId, string dataSource, string timeRange)
        {
            Dictionary<string, dynamic> systemContext = PrepareSystemContext(resource, detectorId, dataSource, timeRange);

            await this._sourceWatcherService.Watcher.WaitForFirstCompletion();
            var dataProviders = new DataProviders.DataProviders((DataProviderContext)this.HttpContext.Items[HostConstants.DataProviderContextKey]);
            var invoker = this._invokerCache.GetSystemInvoker(invokerId);

            if (invoker == null)
            {
                return null;
            }

            Response res = new Response
            {
                Metadata = invoker.EntryPointDefinitionAttribute,
                IsInternalCall = systemContext["isInternal"]
            };

            var response = (Response)await invoker.Invoke(new object[] { dataProviders, systemContext, res });

            List<DataProviderMetadata> dataProvidersMetadata = GetDataProvidersMetadata(dataProviders);

            return response == null ? (IActionResult)NotFound() : Ok(DiagnosticApiResponse.FromCsxResponse(response, dataProvidersMetadata));
        }

        protected async Task<IActionResult> ExecuteQuery<TPostBodyResource>(TResource resource, CompilationPostBody<TPostBodyResource> jsonBody, string startTime, string endTime, string timeGrain, string detectorId = null, string dataSource = null, string timeRange = null, Form Form = null)
        {
            if (jsonBody == null)
            {
                return BadRequest("Missing body");
            }

            if (string.IsNullOrWhiteSpace(jsonBody.Script))
            {
                return BadRequest("Missing script in body");
            }

            if (jsonBody.References == null)
            {
                jsonBody.References = new Dictionary<string, string>();
            }

            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage))
            {
                return BadRequest(errorMessage);
            }

            await _sourceWatcherService.Watcher.WaitForFirstCompletion();

            var runtimeContext = PrepareContext(resource, startTimeUtc, endTimeUtc, Form: Form, detectorId: detectorId);

            var dataProviders = new DataProviders.DataProviders((DataProviderContext)HttpContext.Items[HostConstants.DataProviderContextKey]);

            foreach (var p in _gistCache.GetAllReferences(runtimeContext))
            {
                if (!jsonBody.References.ContainsKey(p.Key))
                {
                    // Add latest version to references
                    jsonBody.References.Add(p);
                }
            }

            if (!Enum.TryParse(jsonBody.EntityType, true, out EntityType entityType))
            {
                entityType = EntityType.Signal;
            }

            QueryResponse<DiagnosticApiResponse> queryRes = new QueryResponse<DiagnosticApiResponse>
            {
                InvocationOutput = new DiagnosticApiResponse()
            };

            string scriptETag = string.Empty;
            if (Request.Headers.ContainsKey("diag-script-etag"))
            {
                scriptETag = Request.Headers["diag-script-etag"];
            }

            string assemblyFullName = string.Empty;
            if (Request.Headers.ContainsKey("diag-assembly-name"))
            {
                assemblyFullName = HttpUtility.UrlDecode(Request.Headers["diag-assembly-name"]);
            }

            string publishingDetectorId = string.Empty;
            if (Request.Headers.ContainsKey("diag-publishing-detector-id"))
            {
                publishingDetectorId = Request.Headers["diag-publishing-detector-id"]; ;
            }

            Assembly tempAsm = null;

            bool isCompilationNeeded = !ScriptCompilation.IsSameScript(jsonBody.Script, scriptETag) || !_assemblyCacheService.IsAssemblyLoaded(assemblyFullName, out tempAsm);
            if (isCompilationNeeded)
            {
                queryRes.CompilationOutput = await _compilerHostClient.GetCompilationResponse(jsonBody.Script, jsonBody.EntityType, jsonBody.References, runtimeContext.OperationContext.RequestId);
            }
            else
            {
                // Setting compilation succeeded to be true as it has been successfully compiled before
                queryRes.CompilationOutput = new CompilerResponse();
                queryRes.CompilationOutput.CompilationSucceeded = true;
                queryRes.CompilationOutput.CompilationTraces = new string[] { "No code changes were detected. Detector code was executed using previous compilation." };
            }

            if (queryRes.CompilationOutput.CompilationSucceeded)
            {
                try
                {
                    if (isCompilationNeeded)
                    {
                        byte[] asmData = Convert.FromBase64String(queryRes.CompilationOutput.AssemblyBytes);
                        byte[] pdbData = Convert.FromBase64String(queryRes.CompilationOutput.PdbBytes);
                        tempAsm = Assembly.Load(asmData, pdbData);
                        queryRes.CompilationOutput.AssemblyName = tempAsm.FullName;
                        _assemblyCacheService.AddAssemblyToCache(tempAsm.FullName, tempAsm);
                    }

                    Request.HttpContext.Response.Headers.Add("diag-script-etag", Convert.ToBase64String(ScriptCompilation.GetHashFromScript(jsonBody.Script)));
                }
                catch (Exception e)
                {
                    throw new Exception($"Problem while loading Assembly: {e.Message}");
                }

                EntityMetadata metaData = new EntityMetadata(jsonBody.Script, entityType);
                using (var invoker = new EntityInvoker(metaData, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports(), jsonBody.References.ToImmutableDictionary()))
                {
                    invoker.InitializeEntryPoint(tempAsm);

                    // Verify Detector with other detectors in the system in case of conflicts
                    List<DataProviderMetadata> dataProvidersMetadata = null;
                    Response invocationResponse = null;
                    bool isInternalCall = true;
                    QueryUtterancesResults utterancesResults = null;

                    if (jsonBody.DetectorUtterances != null && invoker.EntryPointDefinitionAttribute.Description.ToString().Length > 3)
                    {
                        try
                        {
                            // Get suggested utterances for the detector
                            string[] utterances = null;
                            utterances = JsonConvert.DeserializeObject<string[]>(jsonBody.DetectorUtterances);
                            string description = invoker.EntryPointDefinitionAttribute.Description.ToString();
                            var resourceParams = _internalApiHelper.GetResourceParams(invoker.ResourceFilter);
                            var searchUtterances = await _searchService.SearchUtterances(runtimeContext.OperationContext.RequestId, description, utterances, resourceParams);
                            if (searchUtterances != null && searchUtterances.Content != null)
                            {
                                string resultContent = await searchUtterances.Content.ReadAsStringAsync();
                                utterancesResults = JsonConvert.DeserializeObject<QueryUtterancesResults>(resultContent);
                            }
                            else
                            {
                                DiagnosticsETWProvider.Instance.LogInternalAPIHandledException(runtimeContext.OperationContext.RequestId, "SearchServiceReturnedNull", "Search service returned null. This might be because search api is disabled in the project");
                            }
                        }
                        catch (Exception ex)
                        {
                            DiagnosticsETWProvider.Instance.LogInternalAPIHandledException(runtimeContext.OperationContext.RequestId, "SearchAPICallException: QueryUtterances: " + ex.GetType().ToString(), ex.Message);
                            utterancesResults = null;
                        }
                    }

                    try
                    {
                        if (detectorId == null)
                        {
                            if (!VerifyEntity(invoker, ref queryRes, publishingDetectorId)) return Ok(queryRes);
                            RuntimeContext<TResource> cxt = PrepareContext(resource, startTimeUtc, endTimeUtc, Form: Form);

                            var responseInput = new Response()
                            {
                                Metadata = RemovePIIFromDefinition(invoker.EntryPointDefinitionAttribute, cxt.ClientIsInternal),
                                IsInternalCall = cxt.OperationContext.IsInternalCall
                            };
                            invocationResponse = (Response)await invoker.Invoke(new object[] { dataProviders, cxt.OperationContext, responseInput });
                            invocationResponse.UpdateDetectorStatusFromInsights();
                            isInternalCall = cxt.ClientIsInternal;
                        }
                        else
                        {
                            Dictionary<string, dynamic> systemContext = PrepareSystemContext(resource, detectorId, dataSource, timeRange);
                            var responseInput = new Response()
                            {
                                Metadata = invoker.EntryPointDefinitionAttribute,
                                IsInternalCall = systemContext["isInternal"]
                            };
                            invocationResponse = (Response)await invoker.Invoke(new object[] { dataProviders, systemContext, responseInput });
                        }

                        ValidateForms(invocationResponse.Dataset);
                        if (isInternalCall)
                        {
                            dataProvidersMetadata = GetDataProvidersMetadata(dataProviders);
                        }

                        queryRes.RuntimeSucceeded = true;
                        queryRes.InvocationOutput = DiagnosticApiResponse.FromCsxResponse(invocationResponse, dataProvidersMetadata, utterancesResults);
                        queryRes.RuntimeLogOutput = _loggerProvider.GetAndClear(runtimeContext.OperationContext.RequestId);
                    }
                    catch (Exception ex)
                    {
                        if (invocationResponse != null)
                        {
                            runtimeContext.OperationContext.Logger.LogInformation(invocationResponse.ToString());
                        }
                        var baseException = FlattenIfAggregatedException(ex);
                        runtimeContext.OperationContext.Logger.LogError(baseException, "Runtime exception has occurred");
                        queryRes.RuntimeLogOutput = _loggerProvider.GetAndClear(runtimeContext.OperationContext.RequestId);
                        if (isInternalCall)
                        {
                            queryRes.RuntimeSucceeded = false;
                            queryRes.InvocationOutput = CreateQueryExceptionResponse(baseException, invoker.EntryPointDefinitionAttribute, isInternalCall, GetDataProvidersMetadata(dataProviders));
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            return Ok(queryRes);
        }

        protected async Task<IActionResult> PublishPackage(Package pkg)
        {
            if (pkg == null || string.IsNullOrWhiteSpace(pkg.Id))
            {
                return BadRequest();
            }

            await _sourceWatcherService.Watcher.CreateOrUpdatePackage(pkg);
            return Ok();
        }

        private DiagnosticApiResponse CreateQueryExceptionResponse(Exception ex, Definition detectorDefinition, bool isInternal, List<DataProviderMetadata> dataProvidersMetadata)
        {
            Response response = new Response()
            {
                Metadata = RemovePIIFromDefinition(detectorDefinition, isInternal),
                IsInternalCall = isInternal
            };
            response.AddMarkdownView($"<pre><code>Exception message:<strong> {ex.GetType().FullName}: {ex.Message}</strong><br>Stack trace: {ex.StackTrace}</code></pre>", "Detector Runtime Exception");
            return DiagnosticApiResponse.FromCsxResponse(response, dataProvidersMetadata);
        }

        private Exception FlattenIfAggregatedException(Exception ex)
        {
            if (ex is AggregateException)
            {
                var flatten = ((AggregateException)ex).Flatten();
                return flatten.InnerException;
            }

            return ex;
        }

        protected async Task<IActionResult> GetInsights(TResource resource, string pesId, string supportTopicId, string startTime, string endTime, string timeGrain, string supportTopicPath = null, string postBody = null)
        {
            if (supportTopicPath != null)
            {
                SupportTopicModel supportTopicMap = await this._supportTopicService.GetSupportTopicFromString(supportTopicPath, (DataProviderContext)HttpContext.Items[HostConstants.DataProviderContextKey]);
                if (supportTopicMap != null)
                {
                    pesId = supportTopicMap.ProductId;
                    supportTopicId = supportTopicMap.SupportTopicId;
                }
            }
            if (!DateTimeHelper.PrepareStartEndTimeWithTimeGrain(startTime, endTime, timeGrain, out DateTime startTimeUtc, out DateTime endTimeUtc, out TimeSpan timeGrainTimeSpan, out string errorMessage, true))
            {
                return BadRequest(errorMessage);
            }

            supportTopicId = ParseCorrectSupportTopicId(supportTopicId);
            var supportTopic = new SupportTopic() { Id = supportTopicId, PesId = pesId };
            RuntimeContext<TResource> cxt = PrepareContext(resource, startTimeUtc, endTimeUtc, true, supportTopic, null, postBody);
            if (supportTopicId == null)
            {
                DiagnosticsETWProvider.Instance.LogRuntimeHostHandledException(cxt.OperationContext.RequestId, "GetInsights", cxt.OperationContext.Resource.SubscriptionId,
                    cxt.OperationContext.Resource.ResourceGroup, cxt.OperationContext.Resource.Name, "ASCSupportTopicIdNull", "Support Topic Id is null or there is no mapping for the Support topic path provided.");
            }
            DiagnosticsETWProvider.Instance.LogFullAscInsight(cxt.OperationContext.RequestId, "AzureSupportCenter", "ASCAdditionalParameters", postBody);

            List<AzureSupportCenterInsight> insights = null;
            string error = null;
            List<Definition> detectorsRun = new List<Definition>();
            IEnumerable<Definition> allDetectors = null;
            try
            {
                allDetectors = (await ListDetectorsInternal(cxt)).Select(detectorResponse => detectorResponse.Metadata);

                var applicableDetectors = allDetectors
                    .Where(detector => detector.SupportTopicList.FirstOrDefault(st => st.Id == supportTopicId) != null);

                var insightGroups = await Task.WhenAll(applicableDetectors.Select(detector => GetInsightsFromDetector(cxt, detector, detectorsRun)));

                insights = insightGroups.Where(group => group != null).SelectMany(group => group).ToList();
            }
            catch (Exception ex)
            {
                error = ex.GetType().ToString();
                DiagnosticsETWProvider.Instance.LogRuntimeHostHandledException(cxt.OperationContext.RequestId, "GetInsights", cxt.OperationContext.Resource.SubscriptionId,
                    cxt.OperationContext.Resource.ResourceGroup, cxt.OperationContext.Resource.Name, ex.GetType().ToString(), ex.ToString());
            }

            var correlationId = Guid.NewGuid();

            bool defaultInsightReturned = false;

            // Detectors Ran but no insights
            if (!insights.Any() && detectorsRun.Any())
            {
                defaultInsightReturned = true;
                insights.Add(AzureSupportCenterInsightUtilites.CreateDefaultInsight(cxt.OperationContext, detectorsRun));
            }
            else if (!detectorsRun.Any())
            {
                var defaultDetector = allDetectors.FirstOrDefault(detector => detector.Id.StartsWith("default_insights"));
                if (defaultDetector != null)
                {
                    var defaultDetectorInsights = await GetInsightsFromDetector(cxt, defaultDetector, new List<Definition>());
                    defaultInsightReturned = defaultDetectorInsights.Any();
                    insights.AddRange(defaultDetectorInsights);
                }
            }

            var insightInfo = new
            {
                Total = insights.Count,
                Critical = insights.Count(insight => insight.ImportanceLevel == ImportanceLevel.Critical),
                Warning = insights.Count(insight => insight.ImportanceLevel == ImportanceLevel.Warning),
                Info = insights.Count(insight => insight.ImportanceLevel == ImportanceLevel.Info),
                Default = defaultInsightReturned ? 1 : 0
            };

            DiagnosticsETWProvider.Instance.LogRuntimeHostInsightCorrelation(cxt.OperationContext.RequestId, "ControllerBase.GetInsights", cxt.OperationContext.Resource.SubscriptionId,
                cxt.OperationContext.Resource.ResourceGroup, cxt.OperationContext.Resource.Name, correlationId.ToString(), JsonConvert.SerializeObject(insightInfo));

            var response = new AzureSupportCenterInsightEnvelope()
            {
                CorrelationId = correlationId,
                ErrorMessage = error,
                TotalInsightsFound = insights != null ? insights.Count() : 0,
                Insights = insights
            };

            return Ok(response);
        }

        #endregion API Response Methods

        protected TResource GetResource(string subscriptionId, string resourceGroup, string name)
        {
            var subLocationPlacementId = string.Empty;
            if (Request.Headers.TryGetValue(HeaderConstants.SubscriptionLocationPlacementId, out StringValues subscriptionLocationPlacementId))
            {
                subLocationPlacementId = subscriptionLocationPlacementId.FirstOrDefault();
            }
            return (TResource)Activator.CreateInstance(typeof(TResource), subscriptionId, resourceGroup, name, subLocationPlacementId);
        }

        // Purposefully leaving this method in Base class. This method is shared between two resources right now - HostingEnvironment and WebApp
        protected async Task<HostingEnvironment> GetHostingEnvironment(string subscriptionId, string resourceGroup, string name, DiagnosticStampData stampPostBody, DateTime startTime, DateTime endTime, PlatformType? platformType = null)
        {
            if (stampPostBody == null)
            {
                return new HostingEnvironment(subscriptionId, resourceGroup, name);
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


            if (Request.Headers.TryGetValue(HeaderConstants.SubscriptionLocationPlacementId, out StringValues subscriptionLocationPlacementId))
            {
                hostingEnv.SubscriptionLocationPlacementId = subscriptionLocationPlacementId.FirstOrDefault();
            }

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

            if (stampPostBody.Kind == DiagnosticStampType.ASEV1 || stampPostBody.Kind == DiagnosticStampType.ASEV2)
            {
                var result = await this._stampService.GetTenantIdForStamp(stampName, hostingEnv.HostingEnvironmentType == HostingEnvironmentType.None, startTime, endTime, (DataProviderContext)HttpContext.Items[HostConstants.DataProviderContextKey]);
                hostingEnv.PlatformType = result.Item2;
            }
            else
            {
                hostingEnv.PlatformType = platformType ?? PlatformType.Windows;
            }
            
            return hostingEnv;
        }

        private Dictionary<string, dynamic> PrepareSystemContext(TResource resource, string detectorId, string dataSource, string timeRange)
        {
            dataSource = string.IsNullOrWhiteSpace(dataSource) ? "0" : dataSource;
            timeRange = string.IsNullOrWhiteSpace(timeRange) ? "168" : timeRange;

            this.Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues requestIds);

            OperationContext<TResource> cxt = new OperationContext<TResource>(
                resource,
                "",
                "",
                true,
                requestIds.FirstOrDefault()
            );

            _runtimeContext.ClientIsInternal = true;
            _runtimeContext.OperationContext = cxt;

            var invoker = this._invokerCache.GetEntityInvoker<TResource>(detectorId, (RuntimeContext<TResource>)_runtimeContext);
            IEnumerable<SupportTopic> supportTopicList = null;
            Definition definition = null;
            if (invoker != null && invoker.EntryPointDefinitionAttribute != null)
            {
                if (invoker.EntryPointDefinitionAttribute.SupportTopicList != null && invoker.EntryPointDefinitionAttribute.SupportTopicList.Any())
                {
                    supportTopicList = invoker.EntryPointDefinitionAttribute.SupportTopicList;
                }

                definition = invoker.EntryPointDefinitionAttribute;
            }

            Dictionary<string, dynamic> systemContext = new Dictionary<string, dynamic>();
            systemContext.Add("detectorId", detectorId);
            systemContext.Add("requestIds", requestIds);
            systemContext.Add("isInternal", true);
            systemContext.Add("dataSource", dataSource);
            systemContext.Add("timeRange", timeRange);
            systemContext.Add("supportTopicList", supportTopicList);
            systemContext.Add("definition", definition);
            return systemContext;
        }

        private RuntimeContext<TResource> PrepareContext(TResource resource, DateTime startTime, DateTime endTime, bool forceInternal = false, SupportTopic supportTopic = null, Form Form = null, string ascParams = null, string detectorId = null)
        {
            this.Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues requestIds);
            this.Request.Headers.TryGetValue(HeaderConstants.InternalClientHeader, out StringValues internalCallHeader);
            bool isInternalClient = false;
            bool internalViewRequested = false;
            if (internalCallHeader.Any())
            {
                bool.TryParse(internalCallHeader.First(), out isInternalClient);
            }

            if (isInternalClient)
            {
                this.Request.Headers.TryGetValue(HeaderConstants.InternalViewHeader, out StringValues internalViewHeader);
                if (internalViewHeader.Any())
                {
                    bool.TryParse(internalViewHeader.First(), out internalViewRequested);
                }
            }

            var requestId = requestIds.FirstOrDefault();
            var operationContext = new OperationContext<TResource>(
                resource,
                DateTimeHelper.GetDateTimeInUtcFormat(startTime).ToString(DataProviderConstants.KustoTimeFormat),
                DateTimeHelper.GetDateTimeInUtcFormat(endTime).ToString(DataProviderConstants.KustoTimeFormat),
                internalViewRequested || forceInternal,
                requestId,
                supportTopic: supportTopic,
                form: Form,
                cloudDomain: _runtimeContext.CloudDomain,
                ascParams: ascParams,
                logger: _loggerProvider.CreateLogger(requestId)
            );

            if (operationContext.Logger is RuntimeLogger runtimeLogger)
            {
                runtimeLogger.Resource = resource;
                runtimeLogger.DetectorId = detectorId;
            }
            _runtimeContext.ClientIsInternal = isInternalClient || forceInternal;
            _runtimeContext.OperationContext = operationContext;
            var queryParamCollection = Request.Query;
            foreach(var pair in queryParamCollection)
            {
                _runtimeContext.OperationContext.QueryParams.Add(pair.Key, pair.Value);
            }

            return (RuntimeContext<TResource>)_runtimeContext;
        }

        private async Task<IEnumerable<DiagnosticApiResponse>> ListGistsInternal(RuntimeContext<TResource> context)
        {
            await _sourceWatcherService.Watcher.WaitForFirstCompletion();
            return _gistCache.GetEntityInvokerList(context).Select(p => new DiagnosticApiResponse { Metadata = RemovePIIFromDefinition(p.EntryPointDefinitionAttribute, context.ClientIsInternal) });
        }

        private async Task<string> GetGistInternal(string gistId, RuntimeContext<TResource> context)
        {
            await _sourceWatcherService.Watcher.WaitForFirstCompletion();
            var invoker = this._invokerCache.GetEntityInvoker<TResource>(gistId, context);

            if (invoker == null)
            {
                return null;
            }

            return invoker.EntityMetadata.ScriptText;
        }

        private async Task<IEnumerable<DiagnosticApiResponse>> ListDetectorsInternal(RuntimeContext<TResource> context, string queryText = null)
        {
            await this._sourceWatcherService.Watcher.WaitForFirstCompletion();
            var allDetectors = _invokerCache.GetEntityInvokerList<TResource>(context).ToList();

            if (queryText != null && queryText.Length > 1)
            {
                SearchResults searchResults = null;
                var resourceParams = _internalApiHelper.GetResourceParams(context.OperationContext.Resource as IResourceFilter);
                try
                {
                    var res = await _searchService.SearchDetectors(context.OperationContext.RequestId, queryText, resourceParams);
                    if (res.IsSuccessStatusCode && res != null && res.Content != null)
                    {
                        string resultContent = await res.Content.ReadAsStringAsync();
                        searchResults = JsonConvert.DeserializeObject<SearchResults>(resultContent);
                        // Select search results (Detectors) whose score is greater than 0.3 (considering anything below that should not be relevant for the search query)
                        searchResults.Results = searchResults.Results.Where(result => result.Score > 0.3).ToArray();

                        allDetectors.ForEach(detector =>
                        {
                            // Assign the score to detector if it exists in search results, else default to 0
                            var detectorWithScore = (searchResults != null) ? searchResults.Results.FirstOrDefault(x => x.Detector == detector.EntryPointDefinitionAttribute.Id) : null;
                            if (detectorWithScore != null)
                            {
                                detector.EntryPointDefinitionAttribute.Score = detectorWithScore.Score;
                            }
                            else
                            {
                                detector.EntryPointDefinitionAttribute.Score = 0;
                            }
                        });
                        // Finally select only those detectors that have a positive score value
                        allDetectors = allDetectors.Where(x => x.EntryPointDefinitionAttribute.Score > 0).ToList();
                        // Log the filtered public search results
                        var logMessage = new { InsightName = "SearchResultsPublic", InsightData = allDetectors.Select(p => new { Id = p.EntryPointDefinitionAttribute.Id, Score = p.EntryPointDefinitionAttribute.Score }) };
                        DiagnosticsETWProvider.Instance.LogInternalAPIInsights(context.OperationContext.RequestId, JsonConvert.SerializeObject(logMessage));
                    }
                    else
                    {
                        DiagnosticsETWProvider.Instance.LogInternalAPIHandledException(context.OperationContext.RequestId, "SearchServiceReturnedNull", "Search service returned null. This might be because search api is disabled in the project" );
                        return new List<DiagnosticApiResponse>();
                    }
                }
                catch (Exception ex)
                {
                    DiagnosticsETWProvider.Instance.LogInternalAPIHandledException(context.OperationContext.RequestId, "SearchAPICallException: QueryDetectors: " + ex.GetType().ToString(), ex.Message);
                    return new List<DiagnosticApiResponse>();
                }
            }

            return allDetectors.Select(p => new DiagnosticApiResponse { Metadata = RemovePIIFromDefinition(p.EntryPointDefinitionAttribute, context.ClientIsInternal) });
        }

        private async Task<Tuple<Response, List<DataProviderMetadata>>> GetDetectorInternal(string detectorId, RuntimeContext<TResource> context)
        {
            await this._sourceWatcherService.Watcher.WaitForFirstCompletion();
            var queryParams = Request.Query;
            
            var dataProviderContext = (DataProviderContext)HttpContext.Items[HostConstants.DataProviderContextKey];
            
            _kustoMappingCacheService.TryGetValue($"{context.OperationContext.Resource.Provider?.Replace(".", string.Empty)}Configuration",
                out List <Dictionary<string, string>> kustoMappings);

            if (kustoMappings != null)
            {
                dataProviderContext.Configuration.KustoConfiguration.KustoMap = new KustoMap(context.CloudDomain == DataProviderConstants.AzureCloud 
                    ? DataProviderConstants.AzureCloudAlternativeName : context.CloudDomain, kustoMappings);
            }
            
            var dataProviders = new DataProviders.DataProviders(dataProviderContext);
            List<DataProviderMetadata> dataProvidersMetadata = null;
            if (context.ClientIsInternal)
            {
                dataProvidersMetadata = GetDataProvidersMetadata(dataProviders);
            }

            var changeSetId = queryParams.Where(s => s.Key.Equals("changeSetId")).Select(pair => pair.Value);
            if (changeSetId.Any())
            {
                var resourceUriFromQuery = queryParams.Where(s => s.Key.Equals("resourceUri")).Select(pair => pair.Value);
                var resourceUri = context.OperationContext.Resource.ResourceUri;
                var changeSetIdValue = changeSetId.First();
                DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Change set id received - {changeSetIdValue}");
                if (resourceUriFromQuery.Any())
                {
                    resourceUri = resourceUriFromQuery.First();
                }

                // Gets all change sets for the resource uri
                if (changeSetIdValue.Equals("*"))
                {
                    var changesets = await GetAllChangeSets(resourceUri, DateTime.Now.AddDays(-13), DateTime.Now, dataProviders.ChangeAnalysis);
                    return new Tuple<Response, List<DataProviderMetadata>>(changesets, dataProvidersMetadata);
                }

                var changesResponse = await GetChangesByChangeSetId(changeSetIdValue, resourceUri, dataProviders.ChangeAnalysis);
                return new Tuple<Response, List<DataProviderMetadata>>(changesResponse, dataProvidersMetadata);
            }

            var actionQuery = queryParams.Where(s => s.Key.Equals("scanAction")).Select(pair => pair.Value);
            if (actionQuery.Any())
            {
                var scanActionResponse = await HandleScanActionRequest(context.OperationContext.Resource.ResourceUri,
                                                actionQuery.First(),
                                                dataProviders.ChangeAnalysis);
                return new Tuple<Response, List<DataProviderMetadata>>(scanActionResponse, dataProvidersMetadata);
            }

            var invoker = this._invokerCache.GetEntityInvoker<TResource>(detectorId, context);

            if (invoker == null)
            {
                return null;
            }

            var res = new Response
            {
                Metadata = RemovePIIFromDefinition(invoker.EntryPointDefinitionAttribute, context.ClientIsInternal),
                IsInternalCall = context.OperationContext.IsInternalCall
            };

            try
            {
                var response = (Response)await invoker.Invoke(new object[] { dataProviders, context.OperationContext, res });
                response.UpdateDetectorStatusFromInsights();

                return new Tuple<Response, List<DataProviderMetadata>>(response, dataProvidersMetadata);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<IEnumerable<AzureSupportCenterInsight>> GetInsightsFromDetector(RuntimeContext<TResource> context, Definition detector, List<Definition> detectorsRun)
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
                DiagnosticsETWProvider.Instance.LogRuntimeHostHandledException(context.OperationContext.RequestId, "GetInsightsFromDetector", context.OperationContext.Resource.SubscriptionId,
                    context.OperationContext.Resource.ResourceGroup, context.OperationContext.Resource.Name, ex.GetType().ToString(), ex.ToString());
            }

            // Handle Exception or Not Found
            // Not found can occur if invalid detector is put in detector list
            if (response == null)
            {
                return null;
            }

            List<AzureSupportCenterInsight> supportCenterInsights = new List<AzureSupportCenterInsight>();

            if (response.AscInsights.Any())
            {
                foreach (var ascInsight in response.AscInsights)
                {
                    logAscInsight(context, detector, ascInsight);
                    supportCenterInsights.Add(ascInsight);
                }
            }
            else
            {
                var regularToAscInsights = response.Insights.Select(insight =>
                {
                    var ascInsight = AzureSupportCenterInsightUtilites.CreateInsight(insight, context.OperationContext, detector);
                    logAscInsight(context, detector, ascInsight);
                    return ascInsight;
                });
                supportCenterInsights.AddRange(regularToAscInsights);
            }

            var detectorLists = response.Dataset
                .Where(diagnosicData => diagnosicData.RenderingProperties.Type == RenderingType.Detector)
                .SelectMany(diagnosticData => ((DetectorCollectionRendering)diagnosticData.RenderingProperties).DetectorIds)
                .Distinct();

            var allDetectors = await ListDetectorsInternal(context);

            // Check if the detector is of type analysis and if yes, get all the detectors associated with the analysis
            if (detector.Type == DetectorType.Analysis)
            {
                var detectorListAnalysis = allDetectors.Where(detectorResponse => detectorResponse.Metadata.AnalysisTypes != null && detectorResponse.Metadata.AnalysisTypes.Contains(detector.Id, StringComparer.OrdinalIgnoreCase)).Select(d => d.Metadata.Id);
                if (detectorListAnalysis.Any())
                {
                    detectorLists = detectorLists.Union(detectorListAnalysis);
                }
            }

            if (detectorLists.Any())
            {
                var applicableDetectorMetaData = allDetectors.Where(detectorResponse => detectorLists.Contains(detectorResponse.Metadata.Id));
                var detectorListResponses = await Task.WhenAll(applicableDetectorMetaData.Select(detectorResponse => GetInsightsFromDetector(context, detectorResponse.Metadata, detectorsRun)));

                supportCenterInsights.AddRange(detectorListResponses.Where(detectorInsights => detectorInsights != null).SelectMany(detectorInsights => detectorInsights));
            }

            return supportCenterInsights;
        }

        private void logAscInsight(RuntimeContext<TResource> context, Definition detector, AzureSupportCenterInsight ascInsight)
        {
            var loggingContent = new
            {
                supportTopicId = context.OperationContext.SupportTopic.Id,
                pesId = context.OperationContext.SupportTopic.PesId,
                insight = ascInsight
            };

            DiagnosticsETWProvider.Instance.LogFullAscInsight(context.OperationContext.RequestId, detector.Id, ascInsight?.ImportanceLevel.ToString(), JsonConvert.SerializeObject(loggingContent));
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
                    if (metadataProvider != null)
                    {
                        var metadata = metadataProvider.GetMetadata();
                        if (metadata != null)
                        {
                            dataprovidersMetadata.Add(metadata);
                        }
                    }
                }
            }

            return dataprovidersMetadata;
        }

        private bool VerifyEntity(EntityInvoker invoker, ref QueryResponse<DiagnosticApiResponse> queryRes, string publishingDetectorId)
        {
            List<EntityInvoker> allDetectors = this._invokerCache.GetAll().ToList();

            var detectorWithSameId = allDetectors.FirstOrDefault(d => d.EntryPointDefinitionAttribute.Id.Equals(invoker.EntryPointDefinitionAttribute.Id, StringComparison.OrdinalIgnoreCase));
            if (detectorWithSameId != default(EntityInvoker) && publishingDetectorId == HostConstants.NewDetectorId)
            {
                // There exists a detector which has same Id as this one
                queryRes.CompilationOutput.CompilationSucceeded = false;
                queryRes.CompilationOutput.AssemblyBytes = string.Empty;
                queryRes.CompilationOutput.PdbBytes = string.Empty;
                var detectorType = invoker.EntityMetadata.Type > EntityType.Signal ? invoker.EntityMetadata.Type : EntityType.Detector;
                queryRes.CompilationOutput.CompilationTraces = queryRes.CompilationOutput.CompilationTraces.Concat(new List<string>()
                    {
                        $"Error : There is already a {detectorType} (id : {detectorWithSameId.EntryPointDefinitionAttribute.Id}, name : {detectorWithSameId.EntryPointDefinitionAttribute.Name})" +
                        $" for resource type '{detectorWithSameId.ResourceFilter.ResourceType.ToString()}'. System can't have two {detectorType}s with the same Id. "
                    });

                return false;
            }

            if (!string.IsNullOrWhiteSpace(publishingDetectorId) 
                && !publishingDetectorId.Equals(HostConstants.NewDetectorId, StringComparison.OrdinalIgnoreCase) 
                && !invoker.EntryPointDefinitionAttribute.Id.Equals(publishingDetectorId, StringComparison.OrdinalIgnoreCase))
            {
                // User is trying to change the ID of the detector, reject this request
                queryRes.CompilationOutput.CompilationSucceeded = false;
                queryRes.CompilationOutput.AssemblyBytes = string.Empty;
                queryRes.CompilationOutput.PdbBytes = string.Empty;
                var detectorType = invoker.EntityMetadata.Type > EntityType.Signal ? invoker.EntityMetadata.Type : EntityType.Detector;
                queryRes.CompilationOutput.CompilationTraces = queryRes.CompilationOutput.CompilationTraces.Concat(new List<string>()
                    {
                        $"Error : You cannot change the Id attribute for your {detectorType} as that might end up creating a duplicate {detectorType} with the same name. " +
                        $"So copy the code and create a new {detectorType} with a new Id and reach out to AppLens Team to delete the old {detectorType}."
                    });

                return false;
            }

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
                    queryRes.CompilationOutput.CompilationTraces = queryRes.CompilationOutput.CompilationTraces.Concat(new List<string>()
                    {
                        $"Error : There is already a {invoker.EntityMetadata.Type} (id : {existingDetector.EntryPointDefinitionAttribute.Id}, name : {existingDetector.EntryPointDefinitionAttribute.Name})" +
                        $" that uses the SupportTopic (id : {topicId.Id}, pesId : {topicId.PesId}). System can't have two detectors for same support topic id. Consider merging these two detectors."
                    });

                    return false;
                }
            }

            return true;
        }

        private Definition RemovePIIFromDefinition(Definition definition, bool isInternal)
        { 
            string definitionString = JsonConvert.SerializeObject(definition);
            Definition definitionCopy = JsonConvert.DeserializeObject<Definition>(definitionString);
            if (!isInternal)
            {
                definitionCopy.Author = string.Empty;
            }
            return definitionCopy;
        }

        /// <summary>
        /// Validation to check if Form ID is unique and if a form contains a button
        /// </summary>
        private void ValidateForms(List<DiagnosticData> diagnosticDataSet)
        {
            HashSet<int> formIds = new HashSet<int>();
            var detectorForms = diagnosticDataSet.Where(dataset => dataset.RenderingProperties.Type == RenderingType.Form).Select(d => d.Table);
            foreach (DataTable table in detectorForms)
            {
                // Each row has FormID and FormInputs
                foreach (DataRow row in table.Rows)
                {
                    var formId = (int)row[0];
                    if (!formIds.Add(formId))
                    {
                        throw new Exception($"Form ID {formId} already exists. Please give a unique Form ID.");
                    }

                    var formInputs = row[2].CastTo<List<FormInputBase>>();
                    if (!formInputs.Any(input => input.InputType == FormInputTypes.Button))
                    {
                        throw new Exception($"There must at least one button for form id {formId}.");
                    }

                    if (formInputs.Where(input => input.InputType != FormInputTypes.Button).Count() > 5)
                    {
                        throw new Exception("Total number of inputs for a form cannot exceed 5.");
                    }
                }
            }
        }

        /// <summary>
        /// Retreives Changes and returns the data in rows and columns format.
        /// </summary>
        /// <param name="changeSetId">changeSet Id.</param>
        /// <param name="resourceId">Resource Id.</param>
        /// <param name="changeAnalysisDataProvider">Change Analysis Data Provider.</param>
        /// <returns>Changes for given change set id.</returns>
        private async Task<Response> GetChangesByChangeSetId(string changeSetId, string resourceId, IChangeAnalysisDataProvider changeAnalysisDataProvider)
        {
            changeSetId = changeSetId.Replace(" ", "+");
            var changes = await changeAnalysisDataProvider.GetChangesByChangeSetId(changeSetId, resourceId);
            if (changes == null || changes.Count <= 0)
            {
                return new Response();
            }

            Response changesResponse = new Response();
            var table = new DataTable();
            table.TableName = "Changes";
            table.Columns.Add(new DataColumn("TimeStamp", typeof(string)));
            table.Columns.Add(new DataColumn("Category", typeof(string)));
            table.Columns.Add(new DataColumn("Level", typeof(string)));
            table.Columns.Add(new DataColumn("DisplayName", typeof(string)));
            table.Columns.Add(new DataColumn("Description", typeof(string)));
            table.Columns.Add(new DataColumn("OldValue", typeof(string)));
            table.Columns.Add(new DataColumn("NewValue", typeof(string)));
            table.Columns.Add(new DataColumn("InitiatedBy", typeof(string)));
            table.Columns.Add(new DataColumn("JsonPath", typeof(string)));
            changes.ForEach(change =>
            {
                table.Rows.Add(new object[]
                {
                        change.TimeStamp,
                        change.Category,
                        change.Level,
                        change.DisplayName,
                        change.Description,
                        change.OldValue,
                        change.NewValue,
                        change.InitiatedBy,
                        change.JsonPath
                });
            });

            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.ChangesView)
            };

            changesResponse.Dataset.Add(diagData);
            return changesResponse;
        }

        /// <summary>
        /// Submits request to scan for changes and returns response in <see cref="DataTable"/> format.
        /// </summary>
        private async Task<Response> HandleScanActionRequest(string resourceId, string action, IChangeAnalysisDataProvider changeAnalysisDataProvider)
        {
            var scanRequest = await changeAnalysisDataProvider.ScanActionRequest(resourceId, action);
            if (scanRequest == null)
            {
                return new Response();
            }

            Response scanRequestData = new Response();
            var table = new DataTable();
            table.TableName = action;
            table.Columns.Add(new DataColumn("Operation Id", typeof(string)));
            table.Columns.Add(new DataColumn("State", typeof(string)));
            table.Columns.Add(new DataColumn("SubmissionTime", typeof(string)));
            table.Columns.Add(new DataColumn("CompletionTime", typeof(string)));
            table.Rows.Add(new object[]
            {
                scanRequest.OperationId,
                scanRequest.State,
                scanRequest.SubmissionTime,
                scanRequest.CompletionTime
            });

            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.ChangesView)
            };

            scanRequestData.Dataset.Add(diagData);
            return scanRequestData;
        }

        /// <summary>
        /// Retreives all change sets for the resource id.
        /// </summary>
        /// <param name="resourceId">Resource Id.</param>
        /// <param name="changeAnalysisDataProvider">Change Analysis Data Provider.</param>
        /// <returns>All Changesets for the given resource id.</returns>
        private async Task<Response> GetAllChangeSets(string resourceId, DateTime startTime, DateTime endTime, IChangeAnalysisDataProvider changeAnalysisDataProvider)
        {
            var changeSets = await changeAnalysisDataProvider.GetChangeSetsForResource(resourceId, startTime, endTime);
            Response dataSetResponse = new Response();
            dataSetResponse.AddChangeSets(changeSets);
            return dataSetResponse;
        }
    }
}
