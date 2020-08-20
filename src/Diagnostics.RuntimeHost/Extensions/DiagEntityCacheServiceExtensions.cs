using System;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Services.CacheService.Interfaces;
using Diagnostics.RuntimeHost.Services.SourceWatcher;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DiagEntityCacheServiceExtensions
    {
        public static IServiceCollection AddDiagEntitiesTableCacheService(this IServiceCollection services)
        {
            return AddDiagEntitiesTableCacheServiceInternal(services, null);
        }

        public static IServiceCollection AddDiagEntitiesTableCacheService(this IServiceCollection services, IConfiguration configuration)
        {
            return AddDiagEntitiesTableCacheServiceInternal(services, configuration);
        }

        private static IServiceCollection AddDiagEntitiesTableCacheServiceInternal(this IServiceCollection services, IConfiguration configuration)
        {
            if (Enum.Parse<SourceWatcherType>(configuration[$"SourceWatcher:{RegistryConstants.WatcherTypeKey}"]) == SourceWatcherType.AzureStorage)
            {
                services.AddSingleton<IDiagEntityTableCacheService, DiagEntityTableCacheService>();
            }
            else
            {
                services.AddSingleton<IDiagEntityTableCacheService, NullableDiagEntityTableCacheService>();
            }

            return services;
        }
    }
}
