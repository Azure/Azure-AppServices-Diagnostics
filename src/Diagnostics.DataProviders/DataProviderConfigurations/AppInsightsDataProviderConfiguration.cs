namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    [DataSourceConfiguration("AppInsights")]
    public class AppInsightsDataProviderConfiguration : DataProviderConfigurationBase, IDataProviderConfiguration
    {
        [ConfigurationName("EncryptionKey")]
        public string EncryptionKey { get; set; }
    }
}
