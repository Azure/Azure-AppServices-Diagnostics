using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders.Interfaces
{
    public interface IHealthCheck
    {
        Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
