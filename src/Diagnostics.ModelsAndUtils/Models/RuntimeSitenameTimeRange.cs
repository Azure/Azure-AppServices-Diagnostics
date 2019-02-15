using System;
using Newtonsoft.Json;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class RuntimeSitenameTimeRange
    {
        [JsonProperty("runtime_sitename")]
        public string RuntimeSitename { get; set; }

        [JsonProperty("start_time")]
        public DateTime StartTime { get; set; }

        [JsonProperty("end_time")]
        public DateTime EndTime { get; set; }
    }
}
