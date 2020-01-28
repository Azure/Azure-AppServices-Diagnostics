using System;
using System.Collections.Generic;
using System.Text;
using Kusto.Cloud.Platform.Utils;

namespace Diagnostics.DataProviders
{
    public class DataProviderConfigurationBase : IDataProviderConfiguration
    {
        [ConfigurationName("Enabled")]
        public bool Enabled { get; set; }

        [ConfigurationName("HealchCheckInputs")]
        public Dictionary<string, string> HealthCheckInputs { get; set; }

        public virtual void PostInitialize() { }
    }
}
