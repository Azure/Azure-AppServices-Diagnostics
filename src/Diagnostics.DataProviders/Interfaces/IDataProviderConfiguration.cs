using System.Collections.Generic;

namespace Diagnostics.DataProviders
{
    public interface IDataProviderConfiguration
    {
        bool Enabled { get; set; }

        Dictionary<string, string> HealthCheckInputs { get; }

        void PostInitialize();

        void Validate();
    }
}
