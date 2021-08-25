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
using Diagnostics.Scripts.CompilationService.Utilities;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;

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
            this.storageService = (IStorageService)services.GetService(typeof(IStorageService));
            this.devopsService = new DevopsService(configuration);
            this._compilerHostClient = (ICompilerHostClient)services.GetService(typeof(ICompilerHostClient));
            
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
           
            if(!Enum.IsDefined(typeof(DetectorEnvironment), deploymentParameters.DeployEnv))
            {
                return BadRequest("Deployment environment is not valid. ");
            }
            
            DeploymentResponse response = new DeploymentResponse();
            response.DeploymentGuid = Guid.NewGuid().ToString();
            response.DeployedDetectors = new List<string>();
            response.FailedDetectors = new Dictionary<string, string>();

            var commitId = deploymentParameters.CommitId;

            // 3. Get files to compile 
            var filesToCompile = string.IsNullOrWhiteSpace(commitId) ? 
                await this.devopsService.GetFilesBetweenCommits(deploymentParameters)
              : await this.devopsService.GetFilesInCommit(commitId);

            QueryResponse<DiagnosticApiResponse> queryRes = new QueryResponse<DiagnosticApiResponse>
            {
                InvocationOutput = new DiagnosticApiResponse()
            };

            IDictionary<string, string> references = new Dictionary<string, string>();

           

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
                    await storageService.LoadDataToTable(diagEntity, deploymentParameters.DeployEnv);
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
                    var gistContent = await devopsService.GetFileFromBranch($"{gist}/{gist}.csx");
                    references.Add(gist, gistContent);                                                  
                }

                // 3. Otherwise, compile the detector to generate dll.
                queryRes.CompilationOutput = await _compilerHostClient.GetCompilationResponse(file.Content, diagEntity.EntityType, references);  
                
                // 4. If compilation success, save dll to storage container.
                if (queryRes.CompilationOutput.CompilationSucceeded)
                {                                     
                    var blobName = $"{detectorId.ToLower()}/{detectorId.ToLower()}.dll";
                    // 5. Save blob to storage account
                    var etag = await storageService.LoadBlobToContainer(blobName, queryRes.CompilationOutput.AssemblyBytes, deploymentParameters.DeployEnv);                
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
                    await storageService.LoadDataToTable(diagEntity, deploymentParameters.DeployEnv);

                    // 7. update invoker cache for detector. For gists, we dont need to update invoker cache as we pull latest code each time.
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
                    // If compilation fails, add failure reason to the response
                    response.FailedDetectors.Add(detectorId, queryRes.CompilationOutput.CompilationTraces.FirstOrDefault());
                }
            }
            return Ok(response);
        }
    }
}
