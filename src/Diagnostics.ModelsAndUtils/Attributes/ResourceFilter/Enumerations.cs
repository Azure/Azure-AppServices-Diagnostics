using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    public class ArmResourceType
    {
        public ResourceType ResourceType { get; set; }

        public string Provider { get; set; }

        public string ResourceTypeName { get; set; }
    }

    public static class ArmResourceTypes
    {
        public static readonly ArmResourceType App = new ArmResourceType()
        {
            ResourceType = ResourceType.App,
            Provider = "Microsoft.Web",
            ResourceTypeName = "sites"
        };

        public static readonly ArmResourceType HostingEnvironment = new ArmResourceType()
        {
            ResourceType = ResourceType.HostingEnvironment,
            Provider = "Microsoft.Web",
            ResourceTypeName = "hostingEnvironments"
        };

        public static readonly ArmResourceType AppServiceCertificate = new ArmResourceType()
        {
            ResourceType = ResourceType.AppServiceCertificate,
            Provider = "Microsoft.CertificateRegistration",
            ResourceTypeName = "certificateOrders"
        };

        public static readonly ArmResourceType AppServiceDomain = new ArmResourceType()
        {
            ResourceType = ResourceType.AppServiceDomain,
            Provider = "Microsoft.DomainRegistration",
            ResourceTypeName = "domains"
        };

        public static readonly ArmResourceType LogicApp = new ArmResourceType()
        {
            ResourceType = ResourceType.LogicApp,
            Provider = "Microsoft.Logic",
            ResourceTypeName = "workflows"
        };

        public static readonly ArmResourceType ApiManagementService = new ArmResourceType()
        {
            ResourceType = ResourceType.ApiManagementService,
            Provider = "Microsoft.ApiManagement",
            ResourceTypeName = "service"
        };
    }

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
        HostingEnvironment = 2,

        /// <summary>
        /// App Service Certificate Resource
        /// </summary>
        AppServiceCertificate = 4,

        /// <summary>
        /// App Service Domain Resource
        /// </summary>
        AppServiceDomain = 8,

        /// <summary>
        /// Logic App Resource
        /// </summary>
        LogicApp = 16,

        /// <summary>
        /// Api Management Service Resource
        /// </summary>
        ApiManagementService = 32
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
        MobileApp = 8,
        GatewayApp = 16,
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
        None = 0,
        AspNet = 1,
        NetCore = 2,
        Php = 4,
        Python = 8,
        Node = 16,
        Java = 32,
        Static = 64,
        Other = 128,
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
