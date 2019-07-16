namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    [DataSourceConfiguration("CompilerHostAuth")]
    public class CompilerHostAuthConfiguration : IDataProviderConfiguration
    {
        /// <summary>
        /// Client Id. (RuntimeHost)
        /// </summary>
        [ConfigurationName("ClientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// App Key.
        /// </summary>
        [ConfigurationName("AppKey")]
        public string AppKey { get; set; }

        /// <summary>
        /// Microsoft Tenant Id
        /// </summary>
        [ConfigurationName("AADAuthority")]
        public string AADAuthority { get; set; }

        /// <summary>
        /// Resource to issue token - (CompilerHost)
        /// </summary>
        [ConfigurationName("AADResource")]
        public string AADResource { get; set; }

        public void PostInitialize()
        {
            
        }
    }
}
