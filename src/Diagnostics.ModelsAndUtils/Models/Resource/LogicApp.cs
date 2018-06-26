using Diagnostics.ModelsAndUtils.Attributes;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Resource representing logic app
    /// </summary>
    public class LogicApp : LogicAppFilter, IResource
    {
        /// <summary>
        /// Name of the Logic App
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Subscription Id(Guid)
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Resource Group Name
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Determines whether the logic app resource is applicable after filtering.
        /// </summary>
        /// <param name="filter">Resource Filter</param>
        /// <returns>True, if resource passes the filter. False otherwise</returns>
        public bool IsApplicable(IResourceFilter filter)
        {
            return filter is LogicAppFilter;
        }
    }
}
