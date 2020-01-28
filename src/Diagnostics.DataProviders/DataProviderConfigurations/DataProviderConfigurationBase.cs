using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Kusto.Cloud.Platform.Utils;

namespace Diagnostics.DataProviders
{
    public class DataProviderConfigurationBase : IDataProviderConfiguration
    {
        private Dictionary<string, string> _healthCheckInputs;
        private const int keyValuePairLength = 2;

        /// <summary>
        /// Feature flag for data provider.
        /// </summary>
        [ConfigurationName("Enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// A semi-colon delimitted lists of key value pairs. eg., key1=value1;key2=value2;key3=value3
        /// </summary>
        [ConfigurationName("HealthCheckInputs")]
        public string HealthCheckInputsString { get; set; }

        public Dictionary<string, string> HealthCheckInputs
        {
            get
            {
                if (_healthCheckInputs == null)
                {
                    _healthCheckInputs = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
                    if (!string.IsNullOrWhiteSpace(HealthCheckInputsString))
                    {
                        var keyValuePairs = HealthCheckInputsString.Trim().Split(new char[] { ';', ',', '|' });
                        foreach (string pair in keyValuePairs)
                        {
                            var arr = pair.Trim().Split(new char[] { '=' });
                            if (arr.Length == keyValuePairLength)
                            {
                                _healthCheckInputs.Add(arr[0], arr[1]);
                            }
                        }
                    }
                }

                return _healthCheckInputs;
            }
        }

        public virtual void PostInitialize() { }
    }
}
