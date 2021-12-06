using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.DataProviders;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Services.StorageService;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StorageServiceExtensions
    {
        /// <summary>
        /// Add storage services for diag entitities.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddDiagEntitiesStorageService(this IServiceCollection services, IConfiguration configuration)
        {

            if (configuration.IsPublicAzure() || configuration.IsAirGappedCloud())
            {
                services.AddSingleton<IStorageService, StorageService>();
            }
            else if (configuration.IsAzureChinaCloud() || configuration.IsAzureUSGovernment())
            {
                // Sovereign clouds cannot access our StorageService
                services.AddSingleton<IStorageService, NullableStorageService>();
            }
            else
            {
                // IStorageService dependency needs to be initialized for any other cloud including UnitTest
                services.AddSingleton<IStorageService, NullableStorageService>();
            }

            return services;
        }
    }
}
