using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using SourceWatcherFuncApp.Services;
using System.Collections.Generic;
using System.Linq;
using SourceWatcherFuncApp.Utilities;
using Diagnostics.ModelsAndUtils.Models.Storage;
using System.Net;
using Kusto.Cloud.Platform.Utils;
using System.Reflection;

namespace SourceWatcherFuncApp
{
    public class NCloudSourceWatcher
    {
        private IGithubService githubService;

        private IStorageService storageService;

        private IConfigurationRoot config;

        public NCloudSourceWatcher(IConfigurationRoot configurationRoot, IGithubService GithubService, IStorageService StorageService)
        {
            githubService = GithubService;
            config = configurationRoot;
            storageService = StorageService;
        }

        [FunctionName("NCloudSourceWatcher")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function started at: {DateTime.Now}");

            log.LogInformation($"Loading directories from github branch  {config["Github:Branch"]}");

            ServicePointManager.DefaultConnectionLimit = 150;
            var githubDirectories = await githubService.DownloadGithubDirectories(config["Github:Branch"]);
            githubDirectories.ForEach(githubDir =>
            {
                githubDir.Name = githubDir.Name ?? githubDir.Path;
            });

            bool updateEntities = false;
            if (bool.TryParse(config["UpdateEntities"], out bool result))
            {
                updateEntities = result;
            }
            if (updateEntities)
            {

                foreach (var githubdir in githubDirectories)
                {
                    if (!githubdir.Type.Equals("dir", StringComparison.OrdinalIgnoreCase)) continue;

                    try
                    {
                        var contentList = await githubService.DownloadGithubDirectories(branchdownloadUrl: githubdir.Url);
                        var assemblyFile = contentList.Where(githubFile => githubFile.Name.EndsWith("dll")).FirstOrDefault();
                        var scriptFile = contentList.Where(githubfile => githubfile.Name.EndsWith(".csx")).FirstOrDefault();
                        var configFile = contentList.Where(githubFile => githubFile.Name.Equals("package.json", StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                        var metadataFile = contentList.Where(githubFile => githubFile.Name.Equals("metadata.json", StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
 
                        if (assemblyFile != null && scriptFile != null && configFile != null)
                        {

                            log.LogInformation($"Getting content for Assembly file : {assemblyFile.Path}");
                            var doesBlobExists = await storageService.CheckDetectorExists($"{githubdir.Name}/{githubdir.Name}.dll");

                            var assemblyData = await githubService.GetFileContentStream(assemblyFile.Download_url);

                            //log.LogInformation("Reading detector metadata");
                            var configFileData = await githubService.GetFileContentByType<DiagEntity>(configFile.Download_url);

                            configFileData = EntityHelper.PrepareEntityForLoad(assemblyData, string.Empty, configFileData);
                            configFileData.GitHubSha = githubdir.Sha;
                            configFileData.GithubLastModified = await githubService.GetCommitDate(scriptFile.Path);

                            //First check if entity exists in blob or table
                            var existingDetectorEntity = await storageService.GetEntityFromTable(configFileData.PartitionKey, configFileData.RowKey, githubdir.Name);

                            //If there is no entry in table or blob or github last modifed date has been changed, upload to blob
                            if (existingDetectorEntity == null || !doesBlobExists || existingDetectorEntity.GithubLastModified != configFileData.GithubLastModified)
                            {
                                var assemblyLastModified = await githubService.GetCommitDate(assemblyFile.Path);
                                await storageService.LoadBlobToContainer(assemblyFile.Path, assemblyData);
                                await storageService.LoadDataToTable(configFileData, githubdir.Name);
                            }

                            if (!await storageService.CheckDetectorExists(scriptFile.Path))
                            {
                                var scriptText = await githubService.GetFileContentStream(scriptFile.Download_url);
                                await storageService.LoadBlobToContainer(scriptFile.Path, scriptText);
                            }
                        }
                        else
                        {
                            log.LogInformation($"One or more files were not found in {githubdir.Name}, skipped storage entry");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogError($"Exception occured while processing {githubdir.Name}: {ex.ToString()} ");
                    }
                }
            }

            var existingRows = await storageService.GetAllEntities();
            var currentGitHubDirs = githubDirectories.Select(dir => dir.Name).ToList();
            var deletedIds = existingRows.Where(row => !currentGitHubDirs.Contains(row.RowKey.ToLower())).ToList();
            var currentIds = existingRows.Where(row => currentGitHubDirs.Contains(row.RowKey.ToLower())).ToList();
            var updatetasks = new List<Task>();
            deletedIds.ForEach(deletedRow =>
            {
                deletedRow.IsDisabled = true;
                log.LogInformation($"Marking {deletedRow.RowKey} as disabled");
                updatetasks.Add(storageService.LoadDataToTable(deletedRow, ""));
            });

            currentIds.ForEach(currentDetector =>
            {
                currentDetector.IsDisabled = false;
                log.LogInformation($"Marking {currentDetector.RowKey} as active");
                updatetasks.Add(storageService.LoadDataToTable(currentDetector, ""));
            });

            await Task.WhenAll(updatetasks);
            log.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");

            return new OkObjectResult("");
        }
    }
}
