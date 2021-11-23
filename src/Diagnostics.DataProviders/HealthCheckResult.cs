using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Diagnostics.DataProviders
{
    public enum HealthStatus
    {
        Healthy, Unhealthy, Unknown
    }

    /// <summary>
    /// Represents the result of a health check.
    /// </summary>
    public sealed class HealthCheckResult
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public HealthStatus Status { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public Exception Exception { get; private set; }
        public IReadOnlyDictionary<string, object> Data { get; private set; }

        public HealthCheckResult(HealthStatus healthStatus, string healthCheckName, string description = null, Exception ex = null, IReadOnlyDictionary<string, object> data = null)
        {
            Status = healthStatus;
            Name = healthCheckName;
            Description = description;
            Exception = ex;
            Data = data;
        }
    }
}
