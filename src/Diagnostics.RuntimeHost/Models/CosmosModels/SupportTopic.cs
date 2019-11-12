using Newtonsoft.Json;

namespace Diagnostics.RuntimeHost.Models.CosmosModels
{
    public class SupportTopic
    {
        [JsonProperty(PropertyName = "id")]
        public string Id
        {
            get
            {
                return DetectorId + "::" + SupportTopicsId + "::" + PesId;
            }
        }

        [JsonProperty(PropertyName = "detectorId")]
        public string DetectorId { get; set; }

        [JsonProperty(PropertyName = "supportTopicsId")]
        public string SupportTopicsId { get; set; }

        [JsonProperty(PropertyName = "pesId")]
        public string PesId { get; set; }
    }
}
