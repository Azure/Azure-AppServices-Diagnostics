using Diagnostics.ModelsAndUtils.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

namespace SourceWatcherFuncApp.Entities
{
    // Entity that gets stored in the table
    public class DetectorEntity: TableEntity
    {
        public DetectorEntity()
        {

        }
        public DetectorEntity(string entityType, string detectorid)
        {
            PartitionKey = entityType;
            RowKey = detectorid;
        }
        [JsonProperty("id")]
        public string DetectorId { get; set; }
        [JsonProperty("name")]
        public string DetectorName { get; set; }
        [JsonProperty("type")]
        public string EntityType { get; set; }

        public string Author { get; set; }
        public string Description { get; set; }
        public bool IsInternal { get; set; }
        public string Category { get; set; }
        public string ResourceType { get; set; }

        public DateTime GithubLastModified { get; set; }

        public string GitHubSha { get; set; }

        public string SupportTopicListRaw { get; set; }

        public string AnalysisTypesRaw { get; set; }

        [IgnoreProperty]

        public  IEnumerable<SupportTopic> SupportTopicList
        {
            get
            {
                return JsonConvert.DeserializeObject<IEnumerable<SupportTopic>>(SupportTopicListRaw);
            }
            set
            {
                SupportTopicListRaw = JsonConvert.SerializeObject(value);
            }
        }

        [IgnoreProperty]
        public List<string> AnalysisTypes
        {
            get
            {
                return JsonConvert.DeserializeObject<List<string>>(AnalysisTypesRaw);
            }
            set
            {
                AnalysisTypesRaw = JsonConvert.SerializeObject(value);
            }
        }

        public string PlatForm { get; set; }

        public string AppType { get; set; }

        public string StackType { get; set; }

        public string HostingEnvironmentType { get; set; }

        public string ResourceProvider { get; set; }
    }
}
