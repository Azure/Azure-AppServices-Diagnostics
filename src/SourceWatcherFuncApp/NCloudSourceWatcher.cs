using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Diagnostics.Scripts.Models;
using Kusto.Cloud.Platform.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SourceWatcherFuncApp.Services;
using SourceWatcherFuncApp.Utilities;

namespace SourceWatcherFuncApp
{
    public class NCloudSourceWatcher
    {
        private static HttpClient httpClient = new HttpClient();

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
        public async Task Run([TimerTrigger("0 0 */6 * * *")]TimerInfo timerInfo, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function started at: {DateTime.Now}");
            int totalCreates = 0, totalUpdates = 0, totalDeletes = 0 , totalSuccess = 0, totalFailures = 0, totalPackages = 0;

            log.LogInformation($"Loading directories from github branch  {config["Github:Branch"]}");

            ServicePointManager.DefaultConnectionLimit = 150;
            var githubDirectories = await githubService.DownloadGithubDirectories(config["Github:Branch"]);
            totalPackages = githubDirectories.Count();
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
                    if (!githubdir.Type.Equals("tree", StringComparison.OrdinalIgnoreCase)) continue;

                    try
                    {
                        var contentList = await githubService.DownloadGithubDirectories(branchdownloadUrl: githubService.GetContentUrl(githubdir.Name));
                        var assemblyFile = contentList.Where(githubFile => githubFile.Name.EndsWith("dll")).FirstOrDefault();
                        var scriptFile = contentList.Where(githubfile => githubfile.Name.EndsWith(".csx")).FirstOrDefault();
                        var configFile = contentList.Where(githubFile => githubFile.Name.Equals("package.json", StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                        var metadataFile = contentList.Where(githubFile => githubFile.Name.Equals("metadata.json", StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
 
                        if (assemblyFile != null && scriptFile != null && configFile != null)
                        {

                            log.LogInformation($"Getting content for Assembly file : {assemblyFile.Path}");
                            var doesBlobExistsTask = storageService.CheckDetectorExists($"{githubdir.Name}/{githubdir.Name}.dll");
                            var assemblyDataTask = githubService.GetFileContentStream(assemblyFile.Download_url);
                            var configFileDataTask = githubService.GetFileContentByType<DiagEntity>(configFile.Download_url);

                            var doesBlobExists = await doesBlobExistsTask;
                            var assemblyData = await assemblyDataTask;
                            var configFileData = await configFileDataTask;

                            configFileData = EntityHelper.PrepareEntityForLoad(assemblyData, string.Empty, configFileData);
                            configFileData.GitHubSha = githubdir.Sha;
                            configFileData.GithubLastModified = await githubService.GetCommitDate(scriptFile.Path);

                            //First check if entity exists in blob or table
                            var existingDetectorEntity = await storageService.GetEntityFromTable(configFileData.PartitionKey, configFileData.RowKey, githubdir.Name);

                            if (existingDetectorEntity == null)
                            {
                                totalCreates++;
                            }

                            if (existingDetectorEntity != null && existingDetectorEntity.GithubLastModified != configFileData.GithubLastModified)
                            {
                                totalUpdates++;
                            }

                            //If there is no entry in table or blob or github last modifed date has been changed, upload to blob
                            if (existingDetectorEntity == null || !doesBlobExists || (existingDetectorEntity != null && existingDetectorEntity.GithubLastModified != configFileData.GithubLastModified))
                            {
                                await Task.WhenAll(storageService.LoadBlobToContainer(assemblyFile.Path, assemblyData), storageService.LoadDataToTable(configFileData, githubdir.Name)).ConfigureAwait(false);
                            }

                            if (!await storageService.CheckDetectorExists(scriptFile.Path))
                            {
                                var scriptText = await githubService.GetFileContentStream(scriptFile.Download_url);
                                await storageService.LoadBlobToContainer(scriptFile.Path, scriptText).ConfigureAwait(false);
                            }

                            if (configFileData.EntityType.Equals(EntityType.Gist.ToString(), StringComparison.CurrentCultureIgnoreCase))
                            {
                                var commits = await githubService.ListCommitHashes(scriptFile.Path);
                                string path = null;
                                foreach (var commit in commits)
                                {
                                    path = $"{githubdir.Name}/{commit}/{githubdir.Name}.csx";
                                    if (!await storageService.CheckDetectorExists(path))
                                    {
                                        var scriptTextAtCommitSha = await githubService.GetCommitContent(scriptFile.Path, commit);
                                        await storageService.LoadBlobToContainer(path, scriptTextAtCommitSha).ConfigureAwait(false);
                                    }
                                }
                            }
                        }
                        else
                        {
                            log.LogInformation($"One or more files were not found in {githubdir.Name}, skipped storage entry");
                        }

                        totalSuccess++;
                    }
                    catch (Exception ex)
                    {
                        log.LogError($"Exception occured while processing {githubdir.Name}: {ex.ToString()} ");
                        totalFailures++;
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

            totalDeletes = deletedIds.Count;

            currentIds.ForEach(currentDetector =>
            {
                currentDetector.IsDisabled = false;
                log.LogInformation($"Marking {currentDetector.RowKey} as active");
                updatetasks.Add(storageService.LoadDataToTable(currentDetector, ""));
            });

            await Task.WhenAll(updatetasks).ConfigureAwait(false);

            var response = new CallbackResponse
            {
                TotalCreates = totalCreates,
                TotalDeletes = totalDeletes,
                TotalUpdates = totalUpdates,
                TotalSuccess = totalSuccess,
                TotalFailures = totalFailures
            };

            if (!string.IsNullOrWhiteSpace(config["CallbackUrl"]))
            {
                log.LogInformation($"Send results to {config["CallbackUrl"]}");
                var content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, @"application/json");
                await httpClient.PostAsync(config["CallbackUrl"], content).ConfigureAwait(false);
            }

            log.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");
        }
    }

    public class CallbackResponse
    {
        public int TotalCreates { get; set; }
        public int TotalDeletes { get; set; }
        public int TotalFailures { get; set; }
        public int TotalSuccess { get; set; }
        public int TotalUpdates { get; set; }
    }
}
