using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class Category 
    {
        private Category(string categoryName)
        {
            Value = categoryName;
        }

        public string Value { get; private set; }

        public static Category Custom(string name)
        {
            return new Category(name);
        }

        // The predefined categories below are broken out by resource type, 
        // but it does not mean they can only by used by that resource type.

        // General
        public static readonly Category AvailabilityAndPerformance = new Category("Availability and Performance");
        public static readonly Category ConfigurationAndManagement = new Category("Configuration and Management");
        public static readonly Category Deployment = new Category("Deployment");
        public static readonly Category OpenSourceTechnologies = new Category("Open Source Technologies");
        public static readonly Category SSL = new Category("SSL");

        // Site Specific
        public static readonly Category ProblemsWithAse = new Category("Problems with ASE"); // For site on ASE
        public static readonly Category ProblemsWithWebjobs = new Category("Problems with Webjobs");

        // Linux Specific
        public static readonly Category DockerContainers = new Category("DockerContainers");

        // ASE Specific
        public static readonly Category Networking = new Category("Problems with Webjobs");

        // Function Specific
        public static readonly Category DeployingFunctionApps = new Category("Deploying Function Apps");
        public static readonly Category AddingFunctions = new Category("Adding Functions");
        public static readonly Category AuthenticationAndAuthorization = new Category("Authentication and Authorization");
        public static readonly Category ConfiguringAndManagingFunctionApps = new Category("Configuring and Managing Function Apps");
        public static readonly Category DurableFunctions = new Category("Durable Functions");
        public static readonly Category FunctionPortalIssues = new Category("Function Portal Issues");
        public static readonly Category FunctionsPerformance = new Category("Functions Performance");
        public static readonly Category MonitoringFunctions = new Category("Monitoring Functions");

        // Certificates Specific
        public static readonly Category Importing = new Category("Importing");
        public static readonly Category Renewals = new Category("Renewals");

        // Domains Specific
        public static readonly Category Domains = new Category("App Service Domains");
    }
}
