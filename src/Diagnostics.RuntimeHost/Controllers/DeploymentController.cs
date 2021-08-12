using Diagnostics.RuntimeHost.Services.CacheService;
using Microsoft.AspNetCore.Mvc;
using System;
using Diagnostics.RuntimeHost.Services;
using Microsoft.Extensions.Configuration;
using Diagnostics.ModelsAndUtils.Models;
using System.Reflection;
using Diagnostics.RuntimeHost.Services.StorageService;
using System.Threading.Tasks;
using System.Linq;
using Diagnostics.ModelsAndUtils.Models.Storage;
using System.Collections.Generic;
using Diagnostics.Scripts.Models;
using Diagnostics.Scripts;
using Newtonsoft.Json;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Authorization;

namespace Diagnostics.RuntimeHost.Controllers
{
    [Authorize]
    [Produces("application/json")]
    [Route("deploy")]
    public class DeploymentController : Controller
    {

        private DevopsService devopsService;
        protected ICompilerHostClient _compilerHostClient;
        protected IStorageService storageService;
        private IInvokerCacheService detectorCache;

        public DeploymentController(IServiceProvider services, IConfiguration configuration, IInvokerCacheService invokerCache)
        {
            this.devopsService = new DevopsService(configuration);
            this._compilerHostClient = (ICompilerHostClient)services.GetService(typeof(ICompilerHostClient));
            this.storageService = (IStorageService)services.GetService(typeof(IStorageService));
            this.detectorCache = invokerCache;
        }

        [HttpPost("commit")]
        public async Task<IActionResult> Commit([FromBody] DeploymentParameters deploymentParameters)
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

            // 1. Get Partner config from storage
            List<PartnerConfig> partnerConfig = await storageService.GetPartnerConfigsAsync();

            // 2. Initialize Devops client
            devopsService.InitializeClient(partnerConfig.FirstOrDefault());

            // 3. Get files to compile 
            var filesToCompile = string.IsNullOrWhiteSpace(commitId) ? 
                await this.devopsService.GetFilesBetweenCommits(deploymentParameters)
              : await this.devopsService.GetFilesInCommit(commitId);

            QueryResponse<DiagnosticApiResponse> queryRes = new QueryResponse<DiagnosticApiResponse>
            {
                InvocationOutput = new DiagnosticApiResponse()
            };

            // 4. Compile files
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
                    await storageService.LoadDataToTable(diagEntity);
                    if (response.DeletedDetectors == null)
                    {
                        response.DeletedDetectors = new List<string>();
                    }
                    response.DeletedDetectors.Add(diagEntity.RowKey);
                    continue;
                }

                // 3. Otherwise, compile the detector to generate dll.
                queryRes.CompilationOutput = await _compilerHostClient.GetCompilationResponse(file.Content, diagEntity.EntityType, null);  
                
                // 4. If compilation success, save dll to storage container.
                if (queryRes.CompilationOutput.CompilationSucceeded)
                {                                     
                    var blobName = $"{detectorId.ToLower()}/{detectorId.ToLower()}.dll";
                    // 5. Save blob to storage account
                    var etag = await storageService.LoadBlobToContainer(blobName, queryRes.CompilationOutput.AssemblyBytes);                
                    if (string.IsNullOrWhiteSpace(etag))
                    {
                        throw new Exception("Could not save changes");
                    }
                    response.DeployedDetectors.Add(detectorId);

                    // 6. Save entity to table
                    diagEntity.Metadata = file.Metadata;
                    diagEntity.GitHubSha = file.CommitId;
                    byte[] asmData = Convert.FromBase64String(queryRes.CompilationOutput.AssemblyBytes);
                    byte[] pdbData = Convert.FromBase64String(queryRes.CompilationOutput.PdbBytes);
                    diagEntity = DiagEntityHelper.PrepareEntityForLoad(asmData, file.Content, diagEntity);
                    await storageService.LoadDataToTable(diagEntity);

                    // 7. update invoker cache
                    Assembly tempAsm = Assembly.Load(asmData, pdbData);
                    EntityType entityType = EntityType.Detector;
                    EntityMetadata metaData = new EntityMetadata(file.Content, entityType, null);
                    var newInvoker = new EntityInvoker(metaData);
                    newInvoker.InitializeEntryPoint(tempAsm);                   
                    detectorCache.AddOrUpdate(detectorId, newInvoker);
                } else
                {
                    // If compilation fails, add failure reason to the response
                    response.FailedDetectors.Add(detectorId, queryRes.CompilationOutput.CompilationTraces.FirstOrDefault());
                }
            }
            return Ok(response);
        }
    }
}
