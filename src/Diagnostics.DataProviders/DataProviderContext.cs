using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Diagnostics.DataProviders
{
    public class DataProviderContext
    {
        public DataSourcesConfiguration Configuration { get; private set; }
        public string RequestId { get; private set; }
        public CancellationToken DataSourcesCancellationToken { get; private set; }

        public DataProviderContext(DataSourcesConfiguration dataSourceConfiguration, string requestId = null, CancellationToken dataSourceCancellationToken = default(CancellationToken))
        {
            Configuration = dataSourceConfiguration;
            RequestId = requestId ?? Guid.NewGuid().ToString();
            DataSourcesCancellationToken = dataSourceCancellationToken;
        }
    }
}
