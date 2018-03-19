using Diagnostics.DataProviders;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services
{
    public interface IDataSourcesConfigurationService
    {
        DataSourcesConfiguration Config { get; }
    }

    public class DataSourcesConfigurationService : IDataSourcesConfigurationService
    {
        private DataSourcesConfiguration _config;

        public DataSourcesConfiguration Config => _config;

        public DataSourcesConfigurationService(IHostingEnvironment env)
        {
            IConfigurationFactory factory = GetDataProviderConfigurationFactory(env);
            _config = factory.LoadConfigurations();
        }

        public static IConfigurationFactory GetDataProviderConfigurationFactory(IHostingEnvironment env)
        {
            if (env.IsProduction())
            {
                return new RegistryDataProviderConfigurationFactory(RegistryConstants.RegistryRootPath);
            }
            
            switch (env.EnvironmentName.ToLower())
            {
                case "mock":
                    return new MockDataProviderConfigurationFactory();
                default:
                    return new AppSettingsDataProviderConfigurationFactory();
            }
        }
    }
}
