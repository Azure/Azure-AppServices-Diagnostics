using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Diagnostics.DataProviders
{
    public interface IConfigurationFactory
    {
        DataSourcesConfiguration LoadConfigurations();
    }

    public class AppSettingsDataProviderConfigurationFactory : DataProviderConfigurationFactory
    {
        private IConfigurationRoot _configuration;
        public AppSettingsDataProviderConfigurationFactory()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            _configuration = builder.Build();
        }

        protected override string GetValue(string prefix, string name)
        {
            var section = _configuration.GetSection(prefix);
            var appSettingStringValue = section[name];
            return appSettingStringValue;
        }

        private string GetAppSettingName(string prefix, string name)
        {
            return string.Format("{0}_{1}", prefix, name);
        }
    }

    public class RegistryDataProviderConfigurationFactory : DataProviderConfigurationFactory
    {
        private string _registryPath;

        public RegistryDataProviderConfigurationFactory(string registryPath)
        {
            _registryPath = registryPath;
        }

        protected override string GetValue(string prefix, string name)
        {
            string kustoRegistryPath = $@"{_registryPath}\DiagnosticDataProviders\{prefix}";

            return (string)Registry.GetValue(kustoRegistryPath, name, string.Empty);
        }
    }

    public class MockDataProviderConfigurationFactory : DataProviderConfigurationFactory
    {
        protected override string GetValue(string prefix, string name)
        {
            if (prefix == "Kusto")
            {
                switch (name)
                {
                    case "DBName":
                        return "Mock";
                    case "KustoClusterNameGroupings":
                        return "wawsmock";
                    case "KustoRegionGroupings":
                        return "mockstamp";
                    default: return string.Empty;
                }
            }           
            else if (prefix == "SupportObserver" && name == "IsMockConfigured")
            {
                return "true";
            }

            return string.Empty;
        }
    }

    public abstract class DataProviderConfigurationFactory : IConfigurationFactory
    {
        public DataSourcesConfiguration LoadConfigurations()
        {
            var dataSourcesConfiguration = new DataSourcesConfiguration();
            var configurationProperties = dataSourcesConfiguration.GetType().GetProperties()
                    .Where(property => {
                        return property.PropertyType.GetInterfaces().Contains(typeof(IDataProviderConfiguration));
                     });

            foreach(var configProperty in configurationProperties)
            {
                var instance = Activator.CreateInstance(configProperty.PropertyType) as IDataProviderConfiguration;
                LoadConfigurationValues(instance);
                instance.PostInitialize();
                configProperty.SetValue(dataSourcesConfiguration, instance, null);
            }

            return dataSourcesConfiguration;
        }

        private void LoadConfigurationValues(object dataProviderConfiguration)
        {
            string prefix = null;
            DataSourceConfigurationAttribute configurationAttribute = dataProviderConfiguration.GetType()
                .GetCustomAttribute(typeof(DataSourceConfigurationAttribute)) as DataSourceConfigurationAttribute;

            if (configurationAttribute != null && !string.IsNullOrWhiteSpace(configurationAttribute.Prefix))
            {
                prefix = configurationAttribute.Prefix;
            }

            IEnumerable<PropertyInfo> configurationProperties =
                dataProviderConfiguration.GetType().GetProperties()
                    .Where(property => Attribute.IsDefined(property, typeof(ConfigurationNameAttribute)));

            foreach (var property in configurationProperties)
            {
                ConfigurationNameAttribute attribute =
                    Attribute.GetCustomAttribute(property, typeof(ConfigurationNameAttribute)) as ConfigurationNameAttribute;

                if (attribute != null)
                {
                    object existingValue = property.GetValue(dataProviderConfiguration, null);

                    if (!property.PropertyType.IsValueType && existingValue != null)
                    {
                        continue;
                    }

                    var value = GetValue(prefix, attribute.Name);

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        SetValue(dataProviderConfiguration, property, value, attribute.DefaultValue);
                    }
                }
            }
        }

        protected abstract string GetValue(string prefix, string name);

        protected void SetValue(object target, PropertyInfo property, string stringValue, object defaultValue)
        {
            object value = null;
            if (property.PropertyType == typeof(string))
            {
                value = Environment.ExpandEnvironmentVariables(stringValue);
            }
            else if (property.PropertyType == typeof(int))
            {
                int intValue;
                if (!int.TryParse(stringValue, out intValue) && defaultValue != null)
                {
                    value = (int)defaultValue;
                }
                else
                {
                    value = intValue;
                }
            }
            else if (property.PropertyType == typeof(bool))
            {
                bool boolValue;
                if (!bool.TryParse(stringValue, out boolValue) && defaultValue != null)
                {
                    value = (bool)defaultValue;
                }
                else
                {
                    value = boolValue;
                }
            }
            else if (property.PropertyType == typeof(double))
            {
                double doubleValue;
                if (!double.TryParse(stringValue, out doubleValue) && defaultValue != null)
                {
                    value = (double)defaultValue;
                }
                else
                {
                    value = doubleValue;
                }
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Property {0} with type {1} is not supported.",
                        property.Name,
                        property.PropertyType));
            }

            property.SetValue(target, value, null);
        }
    }
}
