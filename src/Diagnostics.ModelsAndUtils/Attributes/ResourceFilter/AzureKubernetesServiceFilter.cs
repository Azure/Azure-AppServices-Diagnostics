namespace Diagnostics.ModelsAndUtils.Attributes
{
    public class AzureKubernetesServiceFilter : ResourceFilterBase
    {
        public AzureKubernetesServiceFilter(bool internalOnly = true) : base(ResourceType.AzureKubernetesService, internalOnly)
        {
        }
    }
}
