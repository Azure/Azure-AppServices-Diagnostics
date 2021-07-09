using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using Kusto.Cloud.Platform.Utils;
using Microsoft.Extensions.Primitives;

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

        public string this[string key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual void PostInitialize() { }

        public virtual void Validate()
        {
            // If the given Data Provider is enabled for Cloud environment, perform validations
            if(Enabled)
            {
                var validationResults = new List<ValidationResult>();
                var context = new ValidationContext(this);
                var valid = Validator.TryValidateObject(this, context, validationResults);
                if (valid) return;

                var msg = string.Join("\n", validationResults.Select(r => r.ErrorMessage));
                throw new Exception($"Invalid configuration detected : {msg}");
            }     
        }
    }
}
