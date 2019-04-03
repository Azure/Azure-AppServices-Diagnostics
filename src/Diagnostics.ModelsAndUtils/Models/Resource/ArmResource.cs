using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.ScriptUtilities;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Resource representing a Generic Arm Resource
    /// </summary>
    public class ArmResource : ArmResourceFilter, IResource
    {
        public string SubscriptionId { get; set; }
        public string ResourceGroup { get; set; }
        public string Name { get; set; }

        public string ResourceUri
        {
            get
            {
                return UriUtilities.BuildAzureResourceUri(SubscriptionId, ResourceGroup, Name, Provider, ResourceTypeName);
            }
        }

        public bool IsApplicable(IResourceFilter filter)
        {
            return filter is ArmResource;
        }

        public ArmResource(string subscriptionId, string resourceGroup, string provider, string resourceTypeName, string resourceName) : base(provider, resourceTypeName)
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroup = resourceGroup;
            this.Name = resourceName;
        }
    }
}