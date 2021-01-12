using Diagnostics.DataProviders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

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

        public DataSourcesConfigurationService(IHostingEnvironment env, IConfiguration configuration)
        {
            IConfigurationFactory factory = GetDataProviderConfigurationFactory(env, configuration);
            _config = factory.LoadConfigurations();
        }

        public static IConfigurationFactory GetDataProviderConfigurationFactory(IHostingEnvironment env, IConfiguration configuration)
        {
            switch (env.EnvironmentName.ToLower())
            {
                case "mock":
                    return new MockDataProviderConfigurationFactory();
                default:
                    return new AppSettingsDataProviderConfigurationFactory(configuration);
            }
        }
    }
}
