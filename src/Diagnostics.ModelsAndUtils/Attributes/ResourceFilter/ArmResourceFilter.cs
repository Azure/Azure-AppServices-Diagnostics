namespace Diagnostics.ModelsAndUtils.Attributes
{
    /// <summary>
    /// Resource representing an arm service
    /// </summary>
    public class ArmResourceFilter : ResourceFilterBase
    {
        /// <summary>
        /// String representing the provider. E.g.. In the following example, Provider = Microsoft.ContainerService
        /// /subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx/resourceGroups/myResourceGroup/providers/Microsoft.ContainerService/managedClusters/myAKSCluster
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// String representing the Resource Type. E.g.. In the following example, ResourceTypeName = managedClusters
        /// /subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx/resourceGroups/myResourceGroup/providers/Microsoft.ContainerService/managedClusters/myAKSCluster
        /// </summary>
        public string ResourceTypeName { get; set; }

        public ArmResourceFilter(string provider, string resourceTypeName, bool internalOnly = true) : base(ResourceType.ArmResource, internalOnly)
        {
            this.Provider = provider;
            this.ResourceTypeName = resourceTypeName;
        }
    }
}
