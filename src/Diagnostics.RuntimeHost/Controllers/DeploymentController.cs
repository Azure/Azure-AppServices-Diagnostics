using Diagnostics.Logger;
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

        public DeploymentController(IStorageService storage, IRepoClient repo, ICompilerHostClient compilerHost, IInvokerCacheService invokerCache)
        {
            this.storageService = storage;
            this.devopsClient = repo;
            this._compilerHostClient = compilerHost;
            this.detectorCache = invokerCache;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DeploymentParameters deploymentParameters)
        {
           
           string validationError = ValidateDeploymentParameters(deploymentParameters);
           if(!string.IsNullOrEmpty(validationError))
            {
                return BadRequest(validationError);
            }
                     
            DeploymentResponse response = new DeploymentResponse();
            response.DeploymentGuid = Guid.NewGuid().ToString();
            response.DeployedDetectors = new List<string>();
            response.FailedDetectors = new Dictionary<string, string>();
           
            var commitId = deploymentParameters.CommitId;
            var timeTakenStopWatch = new Stopwatch();
            timeTakenStopWatch.Start();

            string requestId = HttpContext.Request.Headers[HeaderConstants.RequestIdHeaderName].FirstOrDefault();
            DiagnosticsETWProvider.Instance.LogDeploymentOperationMessage(requestId, response.DeploymentGuid, $"Starting deployment operation");
            //  Get files to compile 
            var filesToCompile = string.IsNullOrWhiteSpace(commitId) ? 
                await this.devopsClient.GetFilesBetweenCommits(deploymentParameters)
              : await this.devopsClient.GetFilesInCommit(commitId, deploymentParameters.ResourceType);

            QueryResponse<DiagnosticApiResponse> queryRes = new QueryResponse<DiagnosticApiResponse>
            {
                InvocationOutput = new DiagnosticApiResponse()
            };

            IDictionary<string, string> references = new Dictionary<string, string>();


            DiagnosticsETWProvider.Instance.LogDeploymentOperationMessage(requestId, response.DeploymentGuid, $"{filesToCompile.Count} to compile");
          
            // Batch update files to be deleted.

            var filesTobeDeleted = filesToCompile.Where(file => file.MarkAsDisabled);
            List<DiagEntity> batchDetectors = new List<DiagEntity>();
            List<DiagEntity> batchGists = new List<DiagEntity>();
            foreach (var file in filesTobeDeleted)
            {
                var diagEntity = JsonConvert.DeserializeObject<DiagEntity>(file.PackageConfig);
                diagEntity.GithubLastModified = DateTime.UtcNow;
                diagEntity.PartitionKey = diagEntity.EntityType;
                diagEntity.RowKey = diagEntity.DetectorId;
                var detectorId = diagEntity.DetectorId;
                DiagnosticsETWProvider.Instance.LogDeploymentOperationMessage(requestId, response.DeploymentGuid, $"Making {detectorId} as disabled");
                diagEntity.IsDisabled = true;
                if (response.DeletedDetectors == null)
                {
                    response.DeletedDetectors = new List<string>();
                }
                response.DeletedDetectors.Add(diagEntity.RowKey);
                if(diagEntity.PartitionKey.Equals("Detector"))
                {
                    batchDetectors.Add(diagEntity);
                } else if (diagEntity.PartitionKey.Equals("Gist"))
                {
                    batchGists.Add(diagEntity);
                }
            }
            // Batch update must share the same partition key, so two separate tasks.
            Task batchDeleteDetectors = batchDetectors.Count > 0 ? storageService.LoadBatchDataToTable(batchDetectors) : Task.CompletedTask;
            Task batchDeleteGists = batchGists.Count > 0 ? storageService.LoadBatchDataToTable(batchGists) : Task.CompletedTask;
            await Task.WhenAll(new Task[] { batchDeleteDetectors, batchDeleteGists });

            foreach ( var file in filesToCompile.Where(file => !file.MarkAsDisabled))
            {
                // For each of the files to compile:
                // 1. Create the diag entity object.
                var diagEntity = JsonConvert.DeserializeObject<DiagEntity>(file.PackageConfig);
                diagEntity.GithubLastModified = DateTime.UtcNow;
                diagEntity.PartitionKey = diagEntity.EntityType;
                diagEntity.RowKey = diagEntity.DetectorId;
                var detectorId = diagEntity.DetectorId;
          
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
                    DiagnosticsETWProvider.Instance.LogDeploymentOperationMessage(requestId, response.DeploymentGuid, $"Saving {blobName} to storage container");
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

                    DiagnosticsETWProvider.Instance.LogDeploymentOperationMessage(requestId, response.DeploymentGuid, $"Saving {diagEntity.RowKey} to storage table");
                    await storageService.LoadDataToTable(diagEntity);

                    DiagnosticsETWProvider.Instance.LogDeploymentOperationMessage(requestId, response.DeploymentGuid, $"Updating invoker cache for {diagEntity.RowKey}");
                    // update invoker cache for detector. For gists, we dont need to update invoker cache as we pull latest code each time.
                    if (diagEntity.PartitionKey.Equals("Detector"))
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
                    DiagnosticsETWProvider.Instance.LogDeploymentOperationMessage(requestId, response.DeploymentGuid, $"Compilation failed for {detectorId}, Reason: {queryRes.CompilationOutput.CompilationTraces.FirstOrDefault()} ");
                    // If compilation fails, add failure reason to the response
                    response.FailedDetectors.Add(detectorId, queryRes.CompilationOutput.CompilationTraces.FirstOrDefault());
                }
            }

            timeTakenStopWatch.Stop();
            DiagnosticsETWProvider.Instance.LogDeploymentOperationMessage(requestId, response.DeploymentGuid, $"Deployment completed for {response.DeploymentGuid}, time elapsed {timeTakenStopWatch.ElapsedMilliseconds}");
            return Ok(response);
        }     
   
        private string ValidateDeploymentParameters(DeploymentParameters deploymentParameters)
        {
            string errorMessage = string.Empty;
            if(string.IsNullOrWhiteSpace(deploymentParameters.ResourceType))
            {
                errorMessage = "Resource Provider Type is invalid. Please provider a valid Resource Provider Type";
            }

            // If all required parameters are empty, reject the request.
            if (string.IsNullOrWhiteSpace(deploymentParameters.CommitId) && string.IsNullOrWhiteSpace(deploymentParameters.FromCommitId)
                && string.IsNullOrWhiteSpace(deploymentParameters.ToCommitId) && string.IsNullOrWhiteSpace(deploymentParameters.StartDate)
                && string.IsNullOrWhiteSpace(deploymentParameters.EndDate))
            {
                errorMessage =  "The given deployment parameters are invalid. Please provide commit id/commit range/date range";
                return errorMessage;
            }

            if(string.IsNullOrWhiteSpace(deploymentParameters.CommitId))
            {
                // validate other parameters
                // Caller has not provided any of the parameters, throw validation error.
                if (string.IsNullOrWhiteSpace(deploymentParameters.StartDate) && string.IsNullOrWhiteSpace(deploymentParameters.EndDate)
                   && string.IsNullOrWhiteSpace(deploymentParameters.FromCommitId) && string.IsNullOrWhiteSpace(deploymentParameters.ToCommitId))
                {
                    errorMessage = "The given deployment parameters are invalid. Please provide both StartDate & EndDate or FromCommit & ToCommit";
                }
                // If both start date & End date are not given, throw validation error.
                if ((string.IsNullOrWhiteSpace(deploymentParameters.StartDate) && !string.IsNullOrWhiteSpace(deploymentParameters.EndDate))
                    || (string.IsNullOrWhiteSpace(deploymentParameters.EndDate) && !string.IsNullOrWhiteSpace(deploymentParameters.StartDate)))
                {
                    errorMessage = "The given deployment parameters are invalid. Please provide both StartDate & EndDate";
                }
                // If both start and end date are present but start date is greater than end date, throw validation error.
                if (!string.IsNullOrWhiteSpace(deploymentParameters.StartDate) && !string.IsNullOrWhiteSpace(deploymentParameters.EndDate)
                    && ((DateTime.Parse(deploymentParameters.StartDate) > DateTime.Parse(deploymentParameters.EndDate))))
                {
                    errorMessage = "Start date cannot be greater than end date";
                }

                // If both FromCommit & ToCommit are not given, throw validation error.
                if ((string.IsNullOrWhiteSpace(deploymentParameters.FromCommitId) && !string.IsNullOrWhiteSpace(deploymentParameters.ToCommitId))
                    || (string.IsNullOrWhiteSpace(deploymentParameters.ToCommitId) && !string.IsNullOrWhiteSpace(deploymentParameters.FromCommitId)))
                {
                    errorMessage = "The given deployment parameters are invalid. Please provide both FromCommitId & ToCommitId";
                } 
            }
            return errorMessage;
        }
    }
}
