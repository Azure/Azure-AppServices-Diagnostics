using Diagnostics.ModelsAndUtils.Models.ChangeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders.Interfaces
{
    public interface IChangeAnalysisClient
    {
        /// <summary>
        /// Gets the ARM Resource Id for given hostnames.
        /// </summary>
        /// <param name="hostnames">array of hostnames</param>
        /// <param name="subscription">subscription id</param>
        Task<ResourceIdResponseModel> GetResourceIdAsync(string[] hostnames, string subscription);

        /// <summary>
        /// Get Change sets for a ResoureceId.
        /// </summary>
        Task<List<ChangeSetResponseModel>> GetChangeSetsAsync(ChangeSetsRequest changeSetsRequest);

        /// <summary>
        /// Gets Changes for a ResoureceId.
        /// </summary>
        Task<List<ResourceChangesResponseModel>> GetChangesAsync(ChangeRequest changeRequest);
    }
}
