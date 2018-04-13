using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    /// <summary>
    /// Enum representing Resource type or collection of Resource Types
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// App Resource
        /// </summary>
        App = 1,

        /// <summary>
        /// Hosting Environment Resource
        /// </summary>
        HostingEnvironment = 2
    }

    /// <summary>
    /// Enum reprensenting Platform type.
    /// </summary>
    public enum PlatformType
    {
        /// <summary>
        /// Reprensents windows platform
        /// </summary>
        Windows = 1,

        /// <summary>
        /// Represents linux platform
        /// </summary>
        Linux = 2
    }

    /// <summary>
    /// Represents different App Types
    /// </summary>
    public enum AppType
    {
        WebApp = 1,
        FunctionApp = 2,
        ApiApp = 4,
        All = 255
    }

    /// <summary>
    /// Hosting Environment Types
    /// </summary>
    public enum HostingEnvironmentType
    {
        None = 0,
        V1 = 1,
        V2 = 2,
        All = 255
    }

    public enum StackType
    {
        AspNet = 1,
        NetCore = 2,
        Php = 4,
        Node = 8,
        Java = 16,
        All = 255
    }

    public enum SkuType
    {
        Free = 1,
        Shared = 2,
        Standard = 4,
        Premium = 8,
        All = 255
    }

    public enum StampType
    {
        Public = 1,
        ASEV1 = 2,
        ASEV2 = 4,
        All = 255
    }
}
