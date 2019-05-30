namespace Diagnostics.ModelsAndUtils.Attributes
{
    /// <summary>
    /// Class representing Filter for App Resource
    /// </summary>
    public class AppFilter : ResourceFilterBase
    {
        /// <summary>
        /// Application Type (Web App, Function App, Api App ...)
        /// </summary>
        public AppType AppType;

        /// <summary>
        /// Platform Type (Windows, Linux ...)
        /// </summary>
        public PlatformType PlatformType;

        /// <summary>
        /// App Stack (ASP.NET/Core, Node, PHP ...)
        /// </summary>
        public StackType StackType;

        /// <summary>
        /// App Stamp (Public Stamp, ASE V1, ASE V2 ...)
        /// </summary>
        public StampType StampType;

        public AppFilter(bool internalOnly = true) : base(ResourceType.App, internalOnly)
        {
            this.AppType = AppType.All;
            this.PlatformType = PlatformType.Windows | PlatformType.Linux;
            this.StackType = StackType.All;
            this.StampType = StampType.All;
        }
    }
}
