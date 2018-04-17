
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
        [ConfigurationName("GeoRegionCertThumbprint")]
        public string GeoRegionCertThumbprint { get; set; }

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
