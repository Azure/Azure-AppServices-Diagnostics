using System;
using System.Collections.Generic;
using System.Text;

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
        
        public HostingEnvironmentFilter(): base(ResourceType.HostingEnvironment)
        {
            this.PlatformType = PlatformType.Windows;
            this.HostingEnvironmentType = HostingEnvironmentType.All;
        }
    }
}
