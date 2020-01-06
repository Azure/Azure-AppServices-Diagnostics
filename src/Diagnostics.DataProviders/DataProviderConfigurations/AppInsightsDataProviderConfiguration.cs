namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    [DataSourceConfiguration("AppInsights")]
    public class AppInsightsDataProviderConfiguration : IDataProviderConfiguration
    {
        [ConfigurationName("EncryptionKey")]
        public string EncryptionKey { get; set; }

        public void PostInitialize()
        {
        }
    }
}
