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
        public DateTime QueryStartTime { get; private set; }
        public DateTime QueryEndTime { get; private set; }
        public CancellationToken DataSourcesCancellationToken { get; private set; }
        public IWawsObserverTokenService WawsObserverTokenService { get; private set; }
        public ISupportBayApiObserverTokenService SupportBayApiObserverTokenService { get; private set; }

        public DataProviderContext(DataSourcesConfiguration dataSourceConfiguration, string requestId = null, CancellationToken dataSourceCancellationToken = default(CancellationToken), DateTime queryStartTime = default(DateTime), DateTime queryEndTime = default(DateTime), IWawsObserverTokenService wawsObserverTokenService = null, ISupportBayApiObserverTokenService supportBayApiObserverTokenService = null)
        {
            Configuration = dataSourceConfiguration;
            RequestId = requestId ?? Guid.NewGuid().ToString();
            DataSourcesCancellationToken = dataSourceCancellationToken;
            QueryStartTime = queryStartTime;
            QueryEndTime = queryEndTime;
            WawsObserverTokenService = wawsObserverTokenService;
            SupportBayApiObserverTokenService = supportBayApiObserverTokenService;
        }
    }
}
