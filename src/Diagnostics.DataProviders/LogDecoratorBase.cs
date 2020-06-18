using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders
{
    public abstract class LogDecoratorBase : IMetadataProvider
    {
        public DiagnosticDataProvider DataProvider { get; private set; }
        protected string _requestId;
        private DataProviderMetadata _metadataProvider;
        private CancellationToken _dataSourceCancellationToken;

        protected LogDecoratorBase(DiagnosticDataProvider dataProvider, DataProviderContext context, DataProviderMetadata metaData)
        {
            DataProvider = dataProvider;
            _metadataProvider = metaData;
            _requestId = context.RequestId;
            _dataSourceCancellationToken = context.DataSourcesCancellationToken;
        }

        protected LogDecoratorBase(DataProviderContext context, DataProviderMetadata metaData)
        {
            _metadataProvider = metaData;
            _requestId = context.RequestId;
            _dataSourceCancellationToken = context.DataSourcesCancellationToken;
        }

        protected async Task<T> MakeDependencyCall<T>(Task<T> dataProviderTask, [CallerMemberName]string dataProviderOperation = "")
        {
            Exception dataProviderException = null;
            DateTime startTime = DateTime.UtcNow, endTime;
            CancellationTokenRegistration cTokenRegistration;

            try
            {
                var tcs = new TaskCompletionSource<bool>();
                using (cTokenRegistration = _dataSourceCancellationToken.Register(() => tcs.TrySetResult(true)))
                {
                    var completedTask = await Task.WhenAny(new Task[] { dataProviderTask, tcs.Task });

                    if (completedTask.Id == dataProviderTask.Id)
                    {
                        return await dataProviderTask;
                    }
                    else
                    {
                        var dataSourceName = this.GetType().Name;
                        var logDecoratorSuffix = "LogDecorator";

                        if (dataSourceName.EndsWith(logDecoratorSuffix, StringComparison.InvariantCultureIgnoreCase))
                        {
                            dataSourceName = dataSourceName.Substring(0, dataSourceName.Length - logDecoratorSuffix.Length);
                        }
                        throw new TimeoutException($"DataSource timed out: {dataSourceName}");
                    }
                }
            }
            catch (Exception ex)
            {
                dataProviderException = ex;
                throw;
            }
            finally
            {
                endTime = DateTime.UtcNow;
                var latencyMilliseconds = Convert.ToInt64((endTime - startTime).TotalMilliseconds);

                if (dataProviderException != null)
                {
                    DiagnosticsETWProvider.Instance.LogDataProviderException(_requestId, dataProviderOperation,
                        startTime.ToString("HH:mm:ss.fff"), endTime.ToString("HH:mm:ss.fff"),
                        latencyMilliseconds, dataProviderException.GetType().ToString(), dataProviderException.ToString());
                }
                else
                {
                    DiagnosticsETWProvider.Instance.LogDataProviderOperationSummary(_requestId, dataProviderOperation, startTime.ToString("HH:mm:ss.fff"),
                        endTime.ToString("HH:mm:ss.fff"), latencyMilliseconds);
                }
            }
        }

        public DataProviderMetadata GetMetadata()
        {
            return _metadataProvider;
        }
    }
}
