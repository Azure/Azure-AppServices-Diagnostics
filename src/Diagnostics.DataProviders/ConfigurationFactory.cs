using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;

namespace Diagnostics.DataProviders
{
    public interface IConfigurationFactory
    {
        DataSourcesConfiguration LoadConfigurations();
    }

    public class AppSettingsDataProviderConfigurationFactory : DataProviderConfigurationFactory
    {
        private IConfigurationRoot _configuration;

        public AppSettingsDataProviderConfigurationFactory(IConfiguration configuration)
        {
            _configuration = (IConfigurationRoot)configuration;
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
            else if (prefix == "K8SELogAnalytics")
            {
                switch (name)
                {
                    case "Provider":
                        return "Mock";
                    case "WorkspaceId":
                        return "MockWorkspaceId";
                    case "ClientId":
                        return "MockClientId";
                    case "ClientSecret":
                        return "MockClientSecret";
                    case "Domain":
                        return "MockDomain";
                    case "AuthEndpoint":
                        return "https://login.microsoftonline.com";
                    case "TokenAudience":
                        return "https://api.loganalytics.io/";
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
                instance.Validate();
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

                    dynamic value;
                    string valueType;
                  
                    if (property.Name == "OverridableExceptions")
                    {
                        List<ITuple> overridableExceptionsList = new List<ITuple>();
                        for (int i = 0; i < int.Parse(GetValue(prefix, "Retry:OverridableExceptionsToRetryAgainstLeaderCluster:AmountOfOverridableExceptions")); i++)
                        {
                            overridableExceptionsList.Add((
                                GetValue(prefix, $"Retry:OverridableExceptionsToRetryAgainstLeaderCluster:Cases:{i.ToString()}:ExceptionString"),
                                double.Parse(GetValue(prefix, $"Retry:OverridableExceptionsToRetryAgainstLeaderCluster:Cases:{i.ToString()}:MaxFailureResponseTimeInSeconds"))));
                        }

                        valueType = "List<ITuple>";
                        value = overridableExceptionsList;
                    }
                    else
                    {
                        valueType = "string";
                        value = GetValue(prefix, attribute.Name);
                    }
                    
                    if (valueType == "string")
                    {
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            SetValue(dataProviderConfiguration, property, value.ToString(), attribute.DefaultValue);
                        }
                    }
                    else if(valueType == "List<ITuple>")
                    {
                        if (value != null)
                        {
                            SetValue(dataProviderConfiguration, property, value, attribute.DefaultValue);
                        }
                    }
                    else
                    {
                        throw new Exception("InvalidType");
                    }
                }
            }
        }

        protected abstract string GetValue(string prefix, string name);

        protected void SetValue(object target, PropertyInfo property, object objectValue, object defaultValue)
        {
            object value = null;
            if (property.PropertyType == typeof(string))
            {
                value = Environment.ExpandEnvironmentVariables(objectValue.ToString());
            }
            else if (property.PropertyType == typeof(List<ITuple>))
            {
                value = objectValue;
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
            /*else if (property.PropertyType == typeof(List<(string, double)>))
            {
                List<(string, double)> listValue;
                if (!List<(string, double)>.TryParse(stringValue, out listValue) && defaultValue != null)
                {
                    value = (double)defaultValue;
                }
                else
                {
                    value = listValue;
                }
            }*/
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
