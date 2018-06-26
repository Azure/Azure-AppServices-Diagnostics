using Diagnostics.ModelsAndUtils.Attributes;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Resource representing App Service Certificate
    /// </summary>
    public class AppServiceCertificate : AppServiceCertificateFilter, IResource
    {
        /// <summary>
        /// Name of the App Service Certificate order
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
        /// Determines whether the App Service certificate resource is applicable after filtering.
        /// </summary>
        /// <param name="filter">Resource Filter</param>
        /// <returns>True, if resource passes the filter. False otherwise</returns>
        public bool IsApplicable(IResourceFilter filter)
        {
            return filter is AppServiceCertificateFilter;
        }
    }
}
