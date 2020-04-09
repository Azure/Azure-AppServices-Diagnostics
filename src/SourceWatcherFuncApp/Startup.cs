using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Options;
using SourceWatcherFuncApp.Services;

[assembly: FunctionsStartup(typeof(ContentModeratorFunction.Startup))]
namespace ContentModeratorFunction
{
    public class Startup : FunctionsStartup
    {
        public IConfigurationRoot Configuration;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var executionContextOptions = builder.Services.BuildServiceProvider().GetService<IOptions<ExecutionContextOptions>>().Value;
            var appDirectory = executionContextOptions.AppDirectory;
            Configuration = new ConfigurationBuilder()
                           .SetBasePath(appDirectory)
                           .AddJsonFile("configuration.json", optional: true, reloadOnChange: true)
                           .AddEnvironmentVariables().Build();
            builder.Services.AddLogging();
            builder.Services.AddSingleton(Configuration);
            builder.Services.AddSingleton<IGithubService, GithubService>();
            builder.Services.AddSingleton<ITableStorageService, TableStorageService>();
            builder.Services.AddSingleton<IBlobService, BlobService>();
        }
    }
}
