using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Diagnostics.ModelsAndUtils.ScriptUtilities;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Resource representing Container App
    /// </summary>
    public class ContainerApp : ContainerAppFilter, IResource
    {
        /// <summary>
        /// Name of the Container App
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// FQDN of the Container App
        /// </summary>
        public string Fqdn { get; set; }

        /// <summary>
        /// Subscription Id(Guid)
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Resource Group Name
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Kube Environment Name
        /// </summary>
        public string KubeEnvironmentName { get; set; }

        /// <summary>
        /// Geo Master Instance Name
        /// </summary>
        public string GeoMasterName { get; set; }

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
                return "Microsoft.Web";
            }
        }

        /// <summary>
        /// Name of Resource Type as defined by ARM resource id. Examples: 'sites', 'hostingEnvironments'
        /// </summary>
        public string ResourceTypeName
        {
            get
            {
                return "containerApps";
            }
        }

        /// <summary>
        /// Subscription Location Placement id
        /// </summary>
        public string SubscriptionLocationPlacementId
        {
            get; set;
        }

        public ContainerApp(string subscriptionId, string resourceGroup, string resourceName, string kubeEnvironmentName=null, string geoMasterName=null, string fqdn=null, string subLocationPlacementId = null) : base()
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroup = resourceGroup;
            this.Name = resourceName;
            this.KubeEnvironmentName = kubeEnvironmentName;
            this.GeoMasterName = geoMasterName;
            this.Fqdn = fqdn;
            SubscriptionLocationPlacementId = subLocationPlacementId;
        }

        /// <summary>
        /// Determines whether the Container App resource is applicable after filtering.
        /// </summary>
        /// <param name="filter">Resource Filter</param>
        /// <returns>True, if resource passes the filter. False otherwise</returns>
        public bool IsApplicable(IResourceFilter filter)
        {
            return base.IsApplicable<ContainerAppFilter>(filter, this.Provider, this.ResourceTypeName);
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
