using System.Collections.Generic;

namespace Diagnostics.DataProviders
{
    public interface IDataProviderConfiguration
    {
        bool Enabled { get; set; }

        Dictionary<string, string> HealthCheckInputs { get; }

        int MaxRetryCount { get; set; }

        int RetryDelayInSeconds { get; set; }
        void PostInitialize();

        void Validate();
    }
}
