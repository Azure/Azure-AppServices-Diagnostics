using System;
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

        /// <summary>
        /// Value of x-ms-client-object-id header received for requests coming from 'Diagnose and Solve' and Applens AppId for requests from Applens.
        /// </summary>
        public string clientObjectId { get; private set; }

        /// <summary>
        /// Value of x-ms-client-principal-name header received for requests coming from 'Diagnose and Solve'.
        /// </summary>
        public string clientPrincipalName { get; private set; }

        public DataProviderContext(DataSourcesConfiguration dataSourceConfiguration, string requestId = null, CancellationToken dataSourceCancellationToken = default(CancellationToken), DateTime queryStartTime = default(DateTime), DateTime queryEndTime = default(DateTime), IWawsObserverTokenService wawsObserverTokenService = null, ISupportBayApiObserverTokenService supportBayApiObserverTokenService = null, string objectId = "", string principalName = "")
        {
            Configuration = dataSourceConfiguration;
            RequestId = requestId ?? Guid.NewGuid().ToString();
            DataSourcesCancellationToken = dataSourceCancellationToken;
            clientObjectId = objectId;
            clientPrincipalName = principalName;
            QueryStartTime = queryStartTime;
            QueryEndTime = queryEndTime;
            WawsObserverTokenService = wawsObserverTokenService;
            SupportBayApiObserverTokenService = supportBayApiObserverTokenService;
        }
    }
}
