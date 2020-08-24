using System;
using System.Collections.Generic;
using Diagnostics.ModelsAndUtils.Attributes;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Diagnostics.ModelsAndUtils.Models.Storage
{
    public class DiagEntity:  TableEntity
    {
        public DiagEntity()
        {

        }
        public DiagEntity(string entityType, string detectorid)
        {
            PartitionKey = entityType;
            RowKey = detectorid;
        }

        /// <summary>
        /// Detector Id/Gist Id
        /// </summary>
        [JsonProperty("id")]
        public string DetectorId { get; set; }

        /// <summary>
        /// Detector name/Gist name
        /// </summary>
        [JsonProperty("name")]
        public string DetectorName { get; set; }

        /// <summary>
        /// Entity type - could be detector, gist
        /// </summary>
        [JsonProperty("type")]
        public string EntityType { get; set; }
        
        public string Metadata { get; set; }

        /// <summary>
        /// Author of the csx file
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Description given by the author in the csx file
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// IsInternal attribute set in the csx file
        /// </summary>
        public bool IsInternal { get; set; }

        /// <summary>
        /// Category of the detector - AvailabilityAndPerf, ConfigManagement
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Azure resource type
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Time detector was last published/edited in Github
        /// </summary>
        public DateTime GithubLastModified { get; set; }

        /// <summary>
        /// Latest github sha of the csx
        /// </summary>
        public string GitHubSha { get; set; }

        /// <summary>
        /// SupportTopicList stored as string in table
        /// </summary>
        public string SupportTopicListRaw { get; set; }

        /// <summary>
        /// AnalysisTypes stored as string in table
        /// </summary>
        public string AnalysisTypesRaw { get; set; }

        /// <summary>
        /// SupportTopic listed defined in the csx
        /// </summary>
        [IgnoreProperty]      
        public IEnumerable<SupportTopic> SupportTopicList
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


        /// <summary>
        /// Analysis types in csx
        /// </summary>
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

        /// <summary>
        /// Platform Type - eg: Windows, Linux
        /// </summary>
        public string PlatForm { get; set; }

        /// <summary>
        /// AppType - WebApp,MobileApp
        /// </summary>
        public string AppType { get; set; }

        /// <summary>
        /// Stack - AspNet,NetCore
        /// </summary>
        public string StackType { get; set; }

        /// <summary>
        /// ASE V1,V2
        /// </summary>
        public string HostingEnvironmentType { get; set; }

        /// <summary>
        ///  Azure Resource Provider
        /// </summary>
        public string ResourceProvider { get; set; }

        /// <summary>
        /// Search result score retrieved from search service.
        /// </summary>
        public float Score { get; set; }

        /// <summary>
        /// Detector type - Detector, Analysis, CategoryOverview
        /// </summary>
        public string DetectorType { get; set; }

        /// <summary>
        /// List of dependencies for a detector
        /// </summary>
        [IgnoreProperty]
        public Dictionary<string, string> Dependencies
        {
            get
            {
                if(string.IsNullOrWhiteSpace(GistReferencesRaw))
                {
                    return null;
                }
                return JsonConvert.DeserializeObject<Dictionary<string,string>>(GistReferencesRaw);
            } 
            set
            {
                if(value == null)
                {
                    GistReferencesRaw = string.Empty;
                } else
                {
                    GistReferencesRaw = JsonConvert.SerializeObject(value);
                }
            }
        }

        /// <summary>
        /// Gist References stored as raw string in table.
        /// </summary>
        public string GistReferencesRaw { get; set; } = string.Empty;

        /// <summary>
        /// Is detector marked for deletion in github.
        /// </summary>
        public bool IsDisabled { get; set; } = false;
    }
}
