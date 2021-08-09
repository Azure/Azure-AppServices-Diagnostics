namespace Diagnostics.ModelsAndUtils.Attributes
{
    public class WorkerAppFilter : ResourceFilterBase
    {
        public WorkerAppFilter(bool internalOnly = true) : base(ResourceType.WorkerApp, internalOnly)
        {
        }
    }
}
