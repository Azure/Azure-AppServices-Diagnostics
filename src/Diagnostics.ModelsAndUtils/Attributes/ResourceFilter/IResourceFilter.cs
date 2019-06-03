namespace Diagnostics.ModelsAndUtils.Attributes
{
    public interface IResourceFilter
    {
        ResourceType ResourceType { get; set; }

        bool InternalOnly { get; set; }
    }
}
