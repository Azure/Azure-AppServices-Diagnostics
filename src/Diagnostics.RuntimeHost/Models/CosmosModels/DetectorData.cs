using System;
using Newtonsoft.Json;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.Scripts.Models;

namespace Diagnostics.RuntimeHost.Models.CosmosModels
{
    public class DetectorData
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "author")]
        public string Author { get; set; }

        [JsonProperty(PropertyName = "category")]
        public string Category { get; set; }

        [JsonProperty(PropertyName = "analysisType")]
        public string AnalysisType { get; set; }

        [JsonProperty(PropertyName = "DetectorType")]
        public DetectorType detectorType { get; set; }

        [JsonProperty(PropertyName = "resourceFilter")]
        public IResourceFilter ResourceFilter { get; set; }

        [JsonProperty(PropertyName = "systemFilterSpecifdied")]
        public bool SystemFilterSpecifdied { get; set; }

        [JsonProperty(PropertyName = "metadata")]
        public string Metadata { get; set; }

        [JsonProperty(PropertyName = "entityType")]
        public EntityType EntityType { get; set; }

        [JsonProperty(PropertyName = "lastUpdated")]
        public DateTime LastUpdated { get; set; }

        [JsonProperty(PropertyName = "isGist")]
        public bool IsGist { get; set; }
    }
}
