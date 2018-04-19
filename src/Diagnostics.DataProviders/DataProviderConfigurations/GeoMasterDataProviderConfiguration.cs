
namespace Diagnostics.DataProviders
{
    [DataSourceConfiguration("GeoMaster")]
    public class GeoMasterDataProviderConfiguration : IDataProviderConfiguration
    {
        
        public void PostInitialize()
        {
        }

        public GeoMasterDataProviderConfiguration()
        {
        }

        /// <summary>
        /// GeomasterCertThumbprint
        /// </summary>
        [ConfigurationName("GeoCertThumbprint")]
        public string GeoCertThumbprint { get; set; }

        /// <summary>
        /// GeomasterEndpoint
        /// </summary>
        [ConfigurationName("GeoEndpointAddress")]
        public string GeoEndpointAddress { get; set; }

        /// <summary>
        /// IsInternal
        /// </summary>
        [ConfigurationName("Token")]
        public string Token { get; set; }
    }
}
