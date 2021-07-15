using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Microsoft.AspNetCore.Mvc;
using System;
using Diagnostics.RuntimeHost.Services;
using Microsoft.Extensions.Configuration;
using Diagnostics.ModelsAndUtils.Models;
using System.Reflection;
using Diagnostics.RuntimeHost.Services.StorageService;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using Diagnostics.ModelsAndUtils.Models.Storage;
using System.Collections.Generic;
using Diagnostics.Scripts.Models;
using Diagnostics.Scripts;
using Newtonsoft.Json;
using Diagnostics.RuntimeHost.Utilities;

namespace Diagnostics.RuntimeHost.Controllers
{
    //  [Authorize]
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
            var commitId = deploymentParameters.CommitId;
            // 1. Get Partner config from storage

            List<PartnerConfig> partnerConfig = await storageService.GetPartnerConfigsAsync();

            // 2. Initialize Devops client

            devopsService.InitializeClient(partnerConfig.FirstOrDefault());

            // 3. Get files to compile 

            var filesToCompile = string.IsNullOrWhiteSpace(commitId) ? 
                await this.devopsService.GetFilesBetweenCommits(deploymentParameters.FromCommitId, deploymentParameters.ToCommitId)
              : await this.devopsService.GetFilesInCommit(commitId);

            QueryResponse<DiagnosticApiResponse> queryRes = new QueryResponse<DiagnosticApiResponse>
            {
                InvocationOutput = new DiagnosticApiResponse()
            };

            // 4. Compile files
            foreach( var file in filesToCompile)
            {
                queryRes.CompilationOutput = await _compilerHostClient.GetCompilationResponse(file.Content, entityType: "Detector", null);

                if (queryRes.CompilationOutput.CompilationSucceeded)
                {
                    // hack right now, ideally get from config
                    var detectorId = String.Join(";", Regex.Matches(file.Path, @"\/(.+?)\/")
                                        .Cast<Match>()
                                        .Select(m => m.Groups[1].Value));

                    var blobName = $"{detectorId.ToLower()}/{detectorId.ToLower()}.dll";

                    // 5. Save blob to storage account
                    var etag = await storageService.LoadBlobToContainer(blobName, queryRes.CompilationOutput.AssemblyBytes);
                    if (string.IsNullOrWhiteSpace(etag))
                    {
                        throw new Exception("Could not save changes");
                    }

                    // 6. Save entity to table

                    var diagEntity = JsonConvert.DeserializeObject<DiagEntity>(file.PackageConfig);
                    diagEntity.Metadata = file.Metadata;
                    diagEntity.GitHubSha = file.CommitId;
                    byte[] asmData = Convert.FromBase64String(queryRes.CompilationOutput.AssemblyBytes);
                    byte[] pdbData = Convert.FromBase64String(queryRes.CompilationOutput.PdbBytes);

                    diagEntity = DiagEntityHelper.PrepareEntityForLoad(asmData, file.Content, diagEntity);
                    await storageService.LoadDataToTable(diagEntity);

                    Assembly tempAsm = Assembly.Load(asmData, pdbData);
                    EntityType entityType = EntityType.Detector;

                    EntityMetadata metaData = new EntityMetadata(file.Content, entityType, null);
                    var newInvoker = new EntityInvoker(metaData);
                    newInvoker.InitializeEntryPoint(tempAsm);

                    // 7. update invoker cache
                    detectorCache.AddOrUpdate(detectorId, newInvoker);

                } else
                {
                    return BadRequest($"Compilation failed for {file.Path} ");
                }

            }
            return Ok($" {commitId} changes saved!");
        }
    }
}
