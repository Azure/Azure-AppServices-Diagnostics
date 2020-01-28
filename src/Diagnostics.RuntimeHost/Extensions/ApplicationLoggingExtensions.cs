using Microsoft.Extensions.Logging.AzureAppServices;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApplicationLoggingExtensions
    {
        /// <summary>
        /// Write application logs to the file system. App Services will honors the settings in the 
        /// App Service logs section of the App Service page of the Azure portal. When Application Logging (Filesystem) setting is updated, the changes take effect immediately without 
        /// requiring a restart or redeployment of the app
        /// </summary>
        /// <param name="services">The Microsoft.Extensions.DependencyInjection.IServiceCollection to add services to.</param>
        /// <returns>The Microsoft.Extensions.DependencyInjection.IServiceCollection so that additional calls can be chained.</returns>
        public static IServiceCollection AddAppServiceApplicationLogging(this IServiceCollection services)
        {
            const int maxLogSizeInBytes = 50 * 1024;
            services.Configure<AzureFileLoggerOptions>(options =>
            {
                options.FileName = "diagnostics-";
                options.FileSizeLimit = maxLogSizeInBytes;
                options.RetainedFileCountLimit = 10;
            });
            return services;
        }
    }
}
