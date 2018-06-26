using Diagnostics.ModelsAndUtils.Attributes;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Resource representing Api Management Service
    /// </summary>
    public class ApiManagementService : ApiManagementServiceFilter, IResource
    {
        /// <summary>
        /// Name of the API Management Service
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

        public ApiManagementService(string subscriptionId, string resourceGroup, string name) : base()
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroup = resourceGroup;
            this.Name = name;
        }

        /// <summary>
        /// Determines whether the APIM resource is applicable after filtering.
        /// </summary>
        /// <param name="filter">Resource Filter</param>
        /// <returns>True, if resource passes the filter. False otherwise</returns>
        public bool IsApplicable(IResourceFilter filter)
        {
            return filter is ApiManagementServiceFilter;
        }
    }
}
