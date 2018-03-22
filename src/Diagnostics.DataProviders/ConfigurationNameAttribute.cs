using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ConfigurationNameAttribute : Attribute
    {
        public ConfigurationNameAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }

        public object DefaultValue { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DataSourceConfigurationAttribute : Attribute
    {
        public DataSourceConfigurationAttribute(string prefix)
        {
            this.Prefix = prefix;
        }

        public string Prefix
        {
            get;
            set;
        }

        public object DefaultValue { get; set; }
    }
}
