using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Diagnostics.ModelsAndUtils.ScriptUtilities;

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
                return "Microsoft.Logic";
            }
        }

        /// <summary>
        /// Name of Resource Type as defined by ARM resource id. Examples: 'sites', 'hostingEnvironments'
        /// </summary>
        public string ResourceTypeName
        {
            get
            {
                return "workflows";
            }
        }

        /// <summary>
        /// Subscription Location Placement id
        /// </summary>
        public string SubscriptionLocationPlacementId
        {
            get; set;
        }

        public LogicApp(string subscriptionId, string resourceGroup, string appName, string subscriptionLocationPlacementid = null) : base()
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroup = resourceGroup;
            this.Name = appName;
            SubscriptionLocationPlacementId = subscriptionLocationPlacementid;
        }

        /// <summary>
        /// Determines whether the logic app resource is applicable after filtering.
        /// </summary>
        /// <param name="filter">Resource Filter</param>
        /// <returns>True, if resource passes the filter. False otherwise</returns>
        public bool IsApplicable(IResourceFilter filter)
        {
            return base.IsApplicable<LogicAppFilter>(filter, this.Provider, this.ResourceTypeName);
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
