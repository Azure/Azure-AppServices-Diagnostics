using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    public class Filter : Attribute
    {
    }

    /// <summary>
    /// Enum representing Resource type or collection of Resource Types
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// Web App Resource
        /// </summary>
        WebApp = 1,

        /// <summary>
        /// Function App Resource
        /// </summary>
        FunctionApp = 2,

        /// <summary>
        /// API App Resource
        /// </summary>
        ApiApp = 4,

        /// <summary>
        /// All App Types
        /// </summary>
        AllApps = 15,

        /// <summary>
        /// Hosting Environment V1
        /// </summary>
        HostingEnvironmentV1 = 16,

        /// <summary>
        /// Hosting Environment V2
        /// </summary>
        HostingEnvironmentV2 = 32,

        /// <summary>
        /// All Hosting Environment Types
        /// </summary>
        HostingEnvironmentAll = 48,
    }

    /// <summary>
    /// Enum reprensenting Platform type.
    /// </summary>
    public enum PlatformType
    {
        /// <summary>
        /// Reprensents windows platform
        /// </summary>
        Windows = 0,

        /// <summary>
        /// Represents linux platform
        /// </summary>
        Linux = 1
    }
}
