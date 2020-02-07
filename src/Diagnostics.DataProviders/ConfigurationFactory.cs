using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Diagnostics.DataProviders.Utility;

namespace Diagnostics.DataProviders
{
    public interface IConfigurationFactory
    {
        DataSourcesConfiguration LoadConfigurations();
    }

    public class AppSettingsDataProviderConfigurationFactory : DataProviderConfigurationFactory
    {
        private IConfigurationRoot _configuration;

        public AppSettingsDataProviderConfigurationFactory(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var builtConfig = builder.Build();

            //var tokenProvider = new AzureServiceTokenProvider(azureAdInstance: builtConfig["Secrets:AzureAdInstance"]);
            /*var keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(
                    tokenProvider.KeyVaultTokenCallback
                )
            );

            string keyVaultConfig = Helpers.GetKeyvaultforEnvironment(env.EnvironmentName);
            builder.AddAzureKeyVault(builtConfig[keyVaultConfig], keyVaultClient, new DefaultKeyVaultSecretManager());*/

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
                        return "wawsmockfollower";
                    case "KustoClusterFailoverGroupings":
                        return "wawsmock";
                    case "KustoRegionGroupings":
                        return "mockstamp";
                    case "HeartBeatConsecutiveFailureLimit":
                    case "HeartBeatConsecutiveSuccessLimit":
                        return "1";
                    case "HeartBeatQuery":
                        return "Heartbeat";
                    case "HeartBeatDelay":
                        return "0";
                    case "AADKustoResource":
                        return "windows.net";
                    default: return string.Empty;
                }
            }
            else if (prefix == "SupportObserver")
            {
                switch (name)
                {
                    case "IsMockConfigured": return "true";
                    case "Endpoint": return "https://wawsobserver.azurewebsites.windows.net";
                    default: return string.Empty;
                }
            }
            else if (prefix == "Mdm")
            {
                switch (name)
                {
                    case "MdmShoeboxEndpoint":
                        return "https://antares.metrics.nsatc.net";
                    case "MdmRegistrationCertThumbprint":
                        // Replace the thumbprint with the certificate installed in your machine.
                        return "";
                    case "MdmShoeboxAccount":
                        return "Mock";
                    default:
                        return string.Empty;
                }
            }
            else if (prefix == "GeoMaster")
            {
                switch (name)
                {
                    case "Token":
                        return "DUMMYTOKEN";
                    default:
                        return string.Empty;
                }
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
                .Where(property =>
                {
                    return property.PropertyType.GetInterfaces().Contains(typeof(IDataProviderConfiguration));
                });

            foreach (var configProperty in configurationProperties)
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
