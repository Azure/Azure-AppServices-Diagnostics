using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Diagnostics.ModelsAndUtils.ScriptUtilities;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Resource representing App Service Domain
    /// </summary>
    public class AppServiceDomain : AppServiceDomainFilter, IResource
    {
        /// <summary>
        /// Name of the App Service Domain
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
        /// Resource URI
        /// </summary>
        public string ResourceUri
        {
            get
            {
                return UriUtilities.BuildAzureResourceUri(SubscriptionId, ResourceGroup, Name, Provider, ResourceTypeName);
            }
        }

        /// <summary>
        /// Arm Resource Provider
        /// </summary>
        public string Provider
        {
            get
            {
                return "Microsoft.DomainRegistration";
            }
        }

        /// <summary>
        /// Name of Resource Type as defined by ARM resource id. Examples: 'sites', 'hostingEnvironments'
        /// </summary>
        public string ResourceTypeName
        {
            get
            {
                return "domains";
            }
        }

        /// <summary>
        /// Subscription Location Placement id
        /// </summary>
        public string SubscriptionLocationPlacementId
        {
            get; set;
        }

        public AppServiceDomain(string subscriptionId, string resourceGroup, string name, string subscriptionLocationPlacementId = null) : base()
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroup = resourceGroup;
            this.Name = name;
            SubscriptionLocationPlacementId = subscriptionLocationPlacementId;
        }

        /// <summary>
        /// Determines whether the App Service domain resource is applicable after filtering.
        /// </summary>
        /// <param name="filter">Resource Filter</param>
        /// <returns>True, if resource passes the filter. False otherwise</returns>
        public bool IsApplicable(IResourceFilter filter)
        {
            return base.IsApplicable<AppServiceDomainFilter>(filter, this.Provider, this.ResourceTypeName);
        }

        /// <summary>
        /// Determines whether the diag entity retrieved from table is applicable after filtering.
        /// </summary>
        /// <param name="diagEntity">Diag Entity from table</param>
        /// <returns>True, if resource passes the filter. False otherwise</returns>
        public bool IsApplicable(DiagEntity diagEntity)
        {
            return base.IsApplicable(diagEntity, this.Provider, this.ResourceTypeName);
        }
    }
}
