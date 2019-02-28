using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Utilities;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Resource representing Azure Kubernetes Service
    /// </summary>
    public class AzureKubernetesService : AzureKubernetesServiceFilter, IResource
    {
        /// <summary>
        /// Name of the Azure Kubernetes Service
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

        public string Provider
        {
            get
            {
                return "Microsoft.ContainerService";
            }
        }

        /// <summary>
        /// Name of Resource Type as defined by ARM resource id. Examples: 'sites', 'hostingEnvironments'
        /// </summary>
        public string ResourceTypeName
        {
            get
            {
                return "managedClusters";
            }
        }

        /// <summary>
        /// Determines whether the Azure Kubernetes Service resource is applicable after filtering.
        /// </summary>
        /// <param name="filter">Resource Filter</param>
        /// <returns>True, if resource passes the filter. False otherwise</returns>
        public bool IsApplicable(IResourceFilter filter)
        {
            return filter is AzureKubernetesServiceFilter;
        }

        public AzureKubernetesService(string subscriptionId, string resourceGroup, string resourceName) : base()
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroup = resourceGroup;
            this.Name = resourceName;
        }
    }
}
