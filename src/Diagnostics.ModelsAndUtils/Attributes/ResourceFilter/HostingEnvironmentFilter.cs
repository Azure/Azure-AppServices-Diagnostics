namespace Diagnostics.ModelsAndUtils.Attributes
{
    /// <summary>
    /// Class representing Filter for Hosting Environment Resource
    /// </summary>
    public class HostingEnvironmentFilter : ResourceFilterBase
    {
        /// <summary>
        /// Platform Type (Windows, Linux ...)
        /// </summary>
        public PlatformType PlatformType;

        /// <summary>
        /// Hosting Environment Type (ASE V1, ASE V2 ...)
        /// </summary>
        public HostingEnvironmentType HostingEnvironmentType;

        public HostingEnvironmentFilter(bool internalOnly = true) : base(ResourceType.HostingEnvironment, internalOnly)
        {
            this.PlatformType = PlatformType.Windows;
            this.HostingEnvironmentType = HostingEnvironmentType.All;
        }
    }
}
