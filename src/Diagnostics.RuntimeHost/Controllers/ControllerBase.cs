using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
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
using System.Reflection;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Controllers
{
    public abstract class ControllerBase : Controller
    {
        protected IStampService _stampService;
        protected ICompilerHostClient _compilerHostClient;
        protected ISourceWatcherService _sourceWatcherService;
        protected IInvokerCacheService _invokerCache;
        protected IDataSourcesConfigurationService _dataSourcesConfigService;

        public ControllerBase(IStampService stampService, ICompilerHostClient compilerHostClient, ISourceWatcherService sourceWatcherService, IInvokerCacheService invokerCache, IDataSourcesConfigurationService dataSourcesConfigService)
        {
            this._stampService = stampService;
            this._compilerHostClient = compilerHostClient;
            this._sourceWatcherService = sourceWatcherService;
            this._invokerCache = invokerCache;
            this._dataSourcesConfigService = dataSourcesConfigService;
        }

        protected OperationContext<TResource> PrepareContext<TResource>(TResource resource, DateTime startTime, DateTime endTime)
            where TResource : IResource
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
                DateTimeHelper.GetDateTimeInUtcFormat(startTime).ToString(HostConstants.KustoTimeFormat),
                DateTimeHelper.GetDateTimeInUtcFormat(endTime).ToString(HostConstants.KustoTimeFormat),
                isInternalRequest,
                requestIds.FirstOrDefault()
            );
        }

        protected async Task<IEnumerable<DiagnosticApiResponse>> ListDetectors<TResource>(OperationContext<TResource> context)
            where TResource : IResource
        {
            await this._sourceWatcherService.Watcher.WaitForFirstCompletion();

            return _invokerCache.GetInvokerList<TResource>(context)
                .Select(p => new DiagnosticApiResponse { Metadata = p.EntryPointDefinitionAttribute });
        }

        protected async Task<IActionResult> GetDetector<TResource>(string detectorId, OperationContext<TResource> context)
            where TResource : IResource
        {
            await this._sourceWatcherService.Watcher.WaitForFirstCompletion();
            var dataProviders = new DataProviders.DataProviders(_dataSourcesConfigService.Config);
            var invoker = this._invokerCache.GetInvoker<TResource>(detectorId, context);

            if (invoker == null)
            {
                return NotFound();
            }

            Response res = new Response
            {
                Metadata = invoker.EntryPointDefinitionAttribute
            };

            var response = (Response)await invoker.Invoke(new object[] { dataProviders, context, res });
            var apiResponse = DiagnosticApiResponse.FromCsxResponse(response);

            return Ok(apiResponse);
        }

        protected async Task<IActionResult> ExecuteQuery<TResource>(string csxScript, OperationContext<TResource> context)
            where TResource : IResource
        {
            await this._sourceWatcherService.Watcher.WaitForFirstCompletion();
            EntityMetadata metaData = new EntityMetadata(csxScript);
            var dataProviders = new DataProviders.DataProviders(_dataSourcesConfigService.Config);

            QueryResponse<DiagnosticApiResponse> queryRes = new QueryResponse<DiagnosticApiResponse>
            {
                InvocationOutput = new DiagnosticApiResponse()
            };

            Assembly tempAsm = null;
            this.Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues requestIds);

            var compilerResponse = await _compilerHostClient.GetCompilationResponse(csxScript, requestIds.FirstOrDefault() ?? string.Empty);

            queryRes.CompilationOutput = compilerResponse;

            if (queryRes.CompilationOutput.CompilationSucceeded)
            {
                byte[] asmData = Convert.FromBase64String(compilerResponse.AssemblyBytes);
                byte[] pdbData = Convert.FromBase64String(compilerResponse.PdbBytes);

                tempAsm = Assembly.Load(asmData, pdbData);

                using (var invoker = new EntityInvoker(metaData, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
                {
                    invoker.InitializeEntryPoint(tempAsm);
                    var responseInput = new Response() { Metadata = invoker.EntryPointDefinitionAttribute };
                    var invocationResponse = (Response)await invoker.Invoke(new object[] { dataProviders, context, responseInput });
                    queryRes.InvocationOutput = DiagnosticApiResponse.FromCsxResponse(invocationResponse);
                }
            }

            return Ok(queryRes);
        }

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
            hostingEnv.TenantIdList = await this._stampService.GetTenantIdForStamp(stampName, startTime, endTime, requestId);
            return hostingEnv;
        }
    }
}
