using System;
using Xunit;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;
using Microsoft.Extensions.Configuration;
using Diagnostics.RuntimeHost.Services.StorageService;
using Microsoft.AspNetCore.Hosting;
using Diagnostics.ModelsAndUtils.Models.Storage;
using System.Diagnostics;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.ModelsAndUtils.Models;
using System.Linq;

namespace Diagnostics.Tests.AzureStorageTests
{
    public class AzureStorageTests: IDisposable
    {

        IStorageService storageService;

        IDiagEntityTableCacheService tableCacheService;

        IConfiguration configuration;

        IHostingEnvironment environment;

        Process process;

        public AzureStorageTests()
        {
            StartStorageEmulator();
            configuration = InitConfig();
            environment = new MockHostingEnvironment();
            environment.EnvironmentName = "UnitTest";
            if(string.IsNullOrWhiteSpace(configuration["SourceWatcher:TableName"]))
            {
                configuration["SourceWatcher:TableName"] = "diagentities";
            }           
            storageService = new StorageService(configuration, environment);
            tableCacheService = new DiagEntityTableCacheService(storageService);
        }

        private void StartStorageEmulator()
        {
            var programFilesPath = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            process = new Process
            {
                StartInfo = {
                UseShellExecute = false,
                FileName = $@"{programFilesPath}\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe",
            }
            };

            bool isRunning = Process.GetProcessesByName("AzureStorageEmulator.exe").Any();
            if(!isRunning)
            {
                StartAndWaitForExit("start");
            }
        }

        public void Dispose()
        {
            StartAndWaitForExit("stop");
        }

        void StartAndWaitForExit(string arguments)
        {
            process.StartInfo.Arguments = arguments;
            process.Start();
        }

        private IConfiguration InitConfig()
        {
             var builder = new ConfigurationBuilder()
                    .AddEnvironmentVariables();
            return builder.Build();
       }

        [Fact]
        /// <summary>
        /// Tests load entity and insert entity
        /// </summary>
        public async void TestTableOperations()
        {          
            // Generate fake entity;
            var diagEntity = new DiagEntity
            {
                PartitionKey = "Detector",
                RowKey = "xyz",
                GithubLastModified = DateTime.UtcNow
            };
            var insertResult = await storageService.LoadDataToTable(diagEntity);
            Assert.NotNull(insertResult);
            var retrieveResult = await storageService.GetEntitiesByPartitionkey("Detector");
            Assert.NotNull(retrieveResult);
            Assert.NotEmpty(retrieveResult);
            var gistResult = await storageService.GetEntitiesByPartitionkey("Gist");
            Assert.Empty(gistResult);
        }

        [Fact]
        /// <summary>
        /// Test if entites are retrieved according to runtime context.
        /// </summary>
        public async void TestCacheOperations()
        {
            var windowsDiagEntity = new DiagEntity
            {
                PartitionKey = "Detector",
                RowKey = "webappDown",
                GithubLastModified = DateTime.UtcNow,
                PlatForm = "Windows",
                ResourceProvider = "Microsoft.Web",
                ResourceType = "sites",
                StackType = "AspNet,NetCore",
                AppType = "WebApp"
            };

            var insertResult = await storageService.LoadDataToTable(windowsDiagEntity);
            var webApp = new App("72383ac7-d6f4-4a5e-bf56-b172f2fdafb2", "resourcegp-default", "diag-test");
            var operationContext = new OperationContext<App>(
                                     webApp,
                                     DateTime.Now.ToString(),
                                     DateTime.Now.AddHours(1).ToString(),
                                     true,
                                     new Guid().ToString()
                                    );
            var context = new RuntimeContext<App>(configuration);
            context.OperationContext = operationContext;

            var detectorsForWebApps = await tableCacheService.GetEntityListByType(context, "Detector");
            Assert.NotNull(detectorsForWebApps);
            Assert.NotEmpty(detectorsForWebApps);

            var logicApp = new LogicApp("72383ac7-d6f4-4a5e-bf56-b172f2fdafb2", "resourcegp-default", "la-test");
            var logicAppOperContext = new OperationContext<LogicApp>(logicApp,
                                             DateTime.Now.ToString(),
                                             DateTime.Now.AddHours(1).ToString(),
                                             true,
                                             new Guid().ToString());

            var runtimeContextLogicApp = new RuntimeContext<LogicApp>(configuration);
            runtimeContextLogicApp.OperationContext = logicAppOperContext;

            var detectorsForLogicApps = await tableCacheService.GetEntityListByType(runtimeContextLogicApp, "Detector");
            Assert.NotNull(detectorsForLogicApps);
            Assert.Empty(detectorsForLogicApps);
        }
    }
}
