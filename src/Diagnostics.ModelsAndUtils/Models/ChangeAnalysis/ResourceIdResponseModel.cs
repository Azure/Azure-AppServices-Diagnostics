using System.Collections.Generic;
using Newtonsoft.Json;
namespace Diagnostics.ModelsAndUtils.Models.ChangeAnalysis
{
    public class ResourceIdResponseModel
    {
        /// <summary>
        /// List of ARM resourceId received from ChangeAnalysis.
        /// Using JsonProperty attribute here for deserialization.
        /// </summary>
        [JsonProperty("values")]
        public List<ResourceIdResponse> ResourceIdResponses;
    }

    public class ResourceIdResponse
    {
        /// <summary>
        /// DNS Hostname of the Resource.
        /// </summary>
        public string Hostname;

        /// <summary>
        /// Azure Resource Id.
        /// </summary>
        public string ResourceId;
    }
}
