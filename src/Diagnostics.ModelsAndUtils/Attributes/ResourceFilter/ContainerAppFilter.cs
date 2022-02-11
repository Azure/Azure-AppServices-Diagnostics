namespace Diagnostics.ModelsAndUtils.Attributes
{
    public class ContainerAppFilter : ResourceFilterBase
    {
        public ContainerAppFilter(bool internalOnly = true) : base(ResourceType.ContainerApp, internalOnly)
        {
        }
    }

    public class MicrosoftAppContainerAppFilter : ResourceFilterBase
    {
        public MicrosoftAppContainerAppFilter(bool internalOnly = true) : base(ResourceType.ContainerApp, internalOnly)
        {
        }
    }
}
