﻿using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Diagnostics.RuntimeHost.Services;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Services.StorageService;
using Diagnostics.RuntimeHost.Services.DevOpsClient;
using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.CompilationService.Utilities;
using Diagnostics.Scripts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Authorize]
    [Produces("application/json")]
    [Route("api/deploy")]
    public class DeploymentController : Controller
    {

        private IRepoClient devopsClient;
        protected ICompilerHostClient _compilerHostClient;
        protected IStorageService storageService;
        private IInvokerCacheService detectorCache;

        public DeploymentController(IServiceProvider services, IConfiguration configuration, IInvokerCacheService invokerCache)
        {
            this.storageService = (IStorageService)services.GetService(typeof(IStorageService));
            this.devopsClient = (IRepoClient)services.GetService(typeof(IRepoClient));
            this._compilerHostClient = (ICompilerHostClient)services.GetService(typeof(ICompilerHostClient));           
            this.detectorCache = invokerCache;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DeploymentParameters deploymentParameters)
        {
            // If all required parameters are empty, reject the request.
            if(string.IsNullOrWhiteSpace(deploymentParameters.CommitId) && string.IsNullOrWhiteSpace(deploymentParameters.FromCommitId)
                && string.IsNullOrWhiteSpace(deploymentParameters.ToCommitId) && string.IsNullOrWhiteSpace(deploymentParameters.StartDate)
                && string.IsNullOrWhiteSpace(deploymentParameters.EndDate)) {
                return BadRequest("The given deployment parameters are invalid");
            }
                     
            DeploymentResponse response = new DeploymentResponse();
            response.DeploymentGuid = Guid.NewGuid().ToString();
            response.DeployedDetectors = new List<string>();
            response.FailedDetectors = new Dictionary<string, string>();

            var commitId = deploymentParameters.CommitId;
            var timeTakenStopWatch = new Stopwatch();
            timeTakenStopWatch.Start();
            DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Starting deployment operation for {response.DeploymentGuid}");
            //  Get files to compile 
            var filesToCompile = string.IsNullOrWhiteSpace(commitId) ? 
                await this.devopsClient.GetFilesBetweenCommits(deploymentParameters)
              : await this.devopsClient.GetFilesInCommit(commitId);

            QueryResponse<DiagnosticApiResponse> queryRes = new QueryResponse<DiagnosticApiResponse>
            {
                InvocationOutput = new DiagnosticApiResponse()
            };

            IDictionary<string, string> references = new Dictionary<string, string>();


            DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Deployment id {response.DeploymentGuid}: {filesToCompile.Count} to compile");
            //  Compile files
            foreach( var file in filesToCompile)
            {
                // For each of the files to compile:
                // 1. Create the diag entity object.
                var diagEntity = JsonConvert.DeserializeObject<DiagEntity>(file.PackageConfig);
                diagEntity.GithubLastModified = DateTime.UtcNow;
                diagEntity.PartitionKey = diagEntity.EntityType;
                diagEntity.RowKey = diagEntity.DetectorId;
                var detectorId = diagEntity.DetectorId;

                // 2. If file was deleted in git, mark as disabled in storage account.
                if (file.MarkAsDisabled)
                {                 
                    diagEntity.IsDisabled = true;
                    DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Making {detectorId} as disabled, deployment operation id: {response.DeploymentGuid}");
                    await storageService.LoadDataToTable(diagEntity);
                    if (response.DeletedDetectors == null)
                    {
                        response.DeletedDetectors = new List<string>();
                    }
                    response.DeletedDetectors.Add(diagEntity.RowKey);
                    continue;
                }

                List<string> gistReferences = DetectorParser.GetLoadDirectiveNames(file.Content);
                
                // Get the latest version of gist from the repo.
                foreach(string gist in gistReferences)
                {                   
                    var gistContent = await devopsClient.GetFileContentAsync($"{gist}/{gist}.csx", deploymentParameters.ResourceType, HttpContext.Request.Headers[HeaderConstants.RequestIdHeaderName]);
                    references.Add(gist, gistContent.ToString());                                                  
                }

                // Otherwise, compile the detector to generate dll.
                queryRes.CompilationOutput = await _compilerHostClient.GetCompilationResponse(file.Content, diagEntity.EntityType, references);  
                
                //  If compilation success, save dll to storage container.
                if (queryRes.CompilationOutput.CompilationSucceeded)
                {                                     
                    var blobName = $"{detectorId.ToLower()}/{detectorId.ToLower()}.dll";
                    DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Deployment id {response.DeploymentGuid}, saving {blobName} to storage container");
                    //  Save blob to storage account
                    var etag = await storageService.LoadBlobToContainer(blobName, queryRes.CompilationOutput.AssemblyBytes);                
                    if (string.IsNullOrWhiteSpace(etag))
                    {
                        throw new Exception($"Could not save changes {blobName} to storage");
                    }
                    response.DeployedDetectors.Add(detectorId);

                    // Save entity to table
                    diagEntity.Metadata = file.Metadata;
                    diagEntity.GitHubSha = file.CommitId;
                    byte[] asmData = Convert.FromBase64String(queryRes.CompilationOutput.AssemblyBytes);
                    byte[] pdbData = Convert.FromBase64String(queryRes.CompilationOutput.PdbBytes);
                    diagEntity = DiagEntityHelper.PrepareEntityForLoad(asmData, file.Content, diagEntity);

                    DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Deployment id {response.DeploymentGuid}, saving {diagEntity.RowKey} to storage table");
                    await storageService.LoadDataToTable(diagEntity);

                    DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Deployment id {response.DeploymentGuid}, updating invoker cache for {diagEntity.RowKey}");
                    // update invoker cache for detector. For gists, we dont need to update invoker cache as we pull latest code each time.
                    if(diagEntity.PartitionKey.Equals("Detector"))
                    {
                        Assembly tempAsm = Assembly.Load(asmData, pdbData);
                        EntityType entityType = EntityType.Detector;
                        EntityMetadata metaData = new EntityMetadata(file.Content, entityType, null);
                        var newInvoker = new EntityInvoker(metaData);
                        newInvoker.InitializeEntryPoint(tempAsm);
                        detectorCache.AddOrUpdate(detectorId, newInvoker);
                    }
                   
                } else
                {
                    DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Deployment id {response.DeploymentGuid}, compilation failed for {detectorId}");
                    // If compilation fails, add failure reason to the response
                    response.FailedDetectors.Add(detectorId, queryRes.CompilationOutput.CompilationTraces.FirstOrDefault());
                }
            }

            timeTakenStopWatch.Stop();
            DiagnosticsETWProvider.Instance.LogRuntimeHostMessage($"Deployment completed for {response.DeploymentGuid}, time elapsed {timeTakenStopWatch.ElapsedMilliseconds}");
            return Ok(response);
        }
    }
}
