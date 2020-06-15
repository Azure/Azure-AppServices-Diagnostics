using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.DataProviders;
using Diagnostics.RuntimeHost.Services.StorageService;
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
            if (configuration.IsPublicAzure())
            {
                services.AddSingleton<IStorageService, StorageService>();
            }
            else
            {
                services.AddSingleton<IStorageService, NullableStorageService>();
            }

            return services;
        }
    }
}
