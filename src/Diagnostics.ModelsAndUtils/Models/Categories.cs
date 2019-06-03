namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// List of predefined problem categories for organizing detectors
    /// Please assign one of these categories to your detector definition attribute
    /// </summary>
    public static class Categories
    {
        // The predefined categories below are broken out by resource type,
        // but it does not mean they can only by used by that resource type.

        // General
        public const string AvailabilityAndPerformance = "Availability and Performance";

        public const string ConfigurationAndManagement = "Configuration and Management";
        public const string Deployment = "Deployment";
        public const string OpenSourceTechnologies = "Open Source Technologies";
        public const string SSL = "SSL";

        // Site Specific
        public const string ProblemsWithAse = "Problems with ASE"; // For site on ASE

        public const string ProblemsWithWebjobs = "Problems with Webjobs";

        // Linux Specific
        public const string DockerContainers = "Docker Containers";

        // ASE Specific
        public const string NetworkConfiguration = "Network Configuration";

        public const string ASEDeployment = "ASE Deployment";
        public const string Scaling = "Scaling";
        public const string Management = "Management";

        // Function Specific
        public const string DeployingFunctionApps = "Deploying Function Apps";

        public const string AddingFunctions = "Adding Functions";
        public const string AuthenticationAndAuthorization = "Authentication and Authorization";
        public const string ConfiguringAndManagingFunctionApps = "Configuring and Managing Function Apps";
        public const string DurableFunctions = "Durable Functions";
        public const string FunctionPortalIssues = "Function Portal Issues";
        public const string FunctionsPerformance = "Functions Performance";
        public const string MonitoringFunctions = "Monitoring Functions";

        // Certificates Specific
        public const string Importing = "Importing";

        public const string Renewals = "Renewals";

        // Domains Specific
        public const string Domains = "App Service Domains";
    }
}
