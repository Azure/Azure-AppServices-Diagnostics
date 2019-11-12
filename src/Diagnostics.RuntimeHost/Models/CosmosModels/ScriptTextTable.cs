using System;
using Newtonsoft.Json;

namespace Diagnostics.RuntimeHost.Models.CosmosModels
{
    public class ScriptTextTable
    {
        [JsonProperty(PropertyName = "id")]
        public string Id
        {
            get
            {
                return DetectorId + Updated.ToString("o");
            }
        }

        [JsonProperty(PropertyName = "detectorId")]
        public string DetectorId { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "comittedBy")]
        public string ComittedBy { get; set; }

        [JsonProperty(PropertyName = "updated")]
        public DateTime Updated { get; set; }
    }
}
