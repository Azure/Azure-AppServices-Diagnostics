using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using SourceWatcherFuncApp.Services;
using SourceWatcherFuncApp.Utilities;
using SourceWatcherFuncApp.Entities;
using System.Net;

namespace Diag.SourceWatcher
{
    public class DiagSourceWatcher
    {        
        private IGithubService githubService;

        private ITableStorageService storageService;

        private IConfigurationRoot config;

        private IBlobService blobService;

        public DiagSourceWatcher(IConfigurationRoot configurationRoot, IGithubService GithubService, 
                                 ITableStorageService StorageService, IBlobService BlobService)
        {
            githubService = GithubService;
            config = configurationRoot;
            storageService = StorageService;
            blobService = BlobService;
        }

        [FunctionName("DiagSourceWatcher")]
        public async Task Run([TimerTrigger("0 0 0 * * *")]TimerInfo timerInfo, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            log.LogInformation($"Loading directories from github branch  {config["Github:Branch"]}");

            ServicePointManager.DefaultConnectionLimit = 150;
            var githubDirectories = await githubService.DownloadGithubDirectories(config["Github:Branch"]);

            foreach (var githubdir in githubDirectories)
            {
                if (!githubdir.Type.Equals("dir", StringComparison.OrdinalIgnoreCase)) continue;

                var contentList = await githubService.DownloadGithubDirectories(branchdownloadUrl: githubdir.Url);

            var assemblyFile = contentList.Where(githubFile => githubFile.Name.EndsWith("dll")).FirstOrDefault();
            var scriptFile = contentList.Where(githubfile => githubfile.Name.EndsWith(".csx")).FirstOrDefault();
            var configFile = contentList.Where(githubFile => githubFile.Name.Equals("package.json", StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

            if (assemblyFile != null && scriptFile != null && configFile != null)
            {

                log.LogInformation($"Getting content for Assembly file : {assemblyFile.Path}");
                var assemblyData = await githubService.GetFileContentStream(assemblyFile.Download_url);

                //log.LogInformation("Reading detector metadata");
                var configFileData = await githubService.GetFileContentByType<DetectorEntity>(configFile.Download_url);

                configFileData = EntityHelper.PrepareEntityForLoad(assemblyData, string.Empty, configFileData);
                configFileData.GitHubSha = githubdir.Sha;
                configFileData.GithubLastModified = await githubService.GetCommitDate(scriptFile.Path);

                //First check if entity exists in blob or table
                var existingDetectorEntity = await storageService.GetEntityFromTable(configFileData.PartitionKey, configFileData.RowKey);
                var doesBlobExists = await blobService.CheckBlobExists(assemblyFile.Path);
                //If there is no entry in table or blob or github last modifed date has been changed, upload to blob
                if (existingDetectorEntity == null || !doesBlobExists || existingDetectorEntity.GithubLastModified != configFileData.GithubLastModified)
                {
                    var assemblyLastModified = await githubService.GetCommitDate(assemblyFile.Path);
                    blobService.LoadBlobToContainer(assemblyFile.Path, assemblyData);
                }
                     var result = await storageService.LoadDataToTable(configFileData);
                }
            else
            {
                log.LogInformation($"One or more files were not found in {githubdir.Name}, skipped storage entry");
                }
            }

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
