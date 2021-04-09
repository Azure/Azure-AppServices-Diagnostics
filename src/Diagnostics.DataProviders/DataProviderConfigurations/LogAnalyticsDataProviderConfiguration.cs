using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Diagnostics.DataProviders.DataProviderConfigurations
{
    public abstract class LogAnalyticsDataProviderConfiguration : DataProviderConfigurationBase, IDataProviderConfiguration
    {
        /// <summary>
        /// Provider
        /// </summary>
        [ConfigurationName("Provider")]
        [Required]
        public string Provider { get; set; }

        /// <summary>
        /// Token Audience
        /// </summary>
        [ConfigurationName("TokenAudience")]
        [Required]
        public string TokenAudience { get; set; }
    }
}
