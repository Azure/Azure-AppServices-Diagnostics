namespace Diagnostics.ModelsAndUtils.Attributes
{
    /// <summary>
    /// Class representing Filter for Logic App Resource
    /// </summary>
    public class LogicAppFilter : ResourceFilterBase
    {
        public LogicAppFilter(bool internalOnly = true) : base(ResourceType.LogicApp, internalOnly)
        {
        }
    }
}
