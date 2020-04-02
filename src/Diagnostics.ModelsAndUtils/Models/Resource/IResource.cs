using Diagnostics.ModelsAndUtils.Attributes;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Interface defining an resource
    /// </summary>
    public interface IResource
    {
        /// <summary>
        /// Subscription Id.
        /// </summary>
        string SubscriptionId { get; set; }

        /// <summary>
        /// Resource Group Name.
        /// </summary>
        string ResourceGroup { get; set; }

        /// <summary>
        /// Resource Name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Resource URI
        /// </summary>
        string ResourceUri { get; }

        /// <summary>
        /// Type of Resource
        /// </summary>
        ResourceType ResourceType { get; set; }

        /// <summary>
        /// Arm Resource Provider
        /// </summary>
        string Provider { get; }

        /// <summary>
        /// Name of Resource Type as defined by ARM resource id. Examples: 'sites', 'hostingEnvironments'
        /// </summary>
        string ResourceTypeName { get; }

        /// <summary>
        /// Determines whether the resource is applicable after filtering.
        /// </summary>
        /// <param name="filter">Resource Filter</param>
        /// <returns>True, if resource passes the filter. False otherwise</returns>
        bool IsApplicable(IResourceFilter filter);
    }
}
