using System.Collections.Generic;

namespace Diagnostics.DataProviders
{
    public interface IDataProviderConfiguration
    {
        bool Enabled { get; set; }

#pragma warning disable CA2227 // Collection properties should be read only
        Dictionary<string, string> HealthCheckInputs { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        void PostInitialize();
    }
}
