namespace Diagnostics.ModelsAndUtils.Attributes
{
    public class ArmResourceFilter : ResourceFilterBase
    {
        public string Provider { get; set; }
        public string ResourceTypeName { get; set; }

        public ArmResourceFilter(string provider, string resourceTypeName, bool internalOnly = true) : base(ResourceType.ArmResource, internalOnly)
        {
            this.Provider = provider;
            this.ResourceTypeName = resourceTypeName;
        }
    }
}