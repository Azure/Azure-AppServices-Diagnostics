using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Diagnostics.Logger;

namespace Diagnostics.DataProviders.Utility
{
    public static class RetryHelper
    {
        public static async Task<TResult> RetryAsync<TResult>(Func<object, Task<TResult>> taskProvider, object funcParam, string source = "", string requestId = "", int maxRetries = 3, int retryDelayInMs = 500)
        {
            return await RetryAsyncBasic<TResult>(taskProvider, source, requestId, maxRetries, retryDelayInMs, funcParam);
        }

        public static async Task<TResult> RetryAsync<TResult>(Func<Task<TResult>> taskProvider, string source = "", string requestId = "", int maxRetries = 3, int retryDelayInMs = 500)
        {
            return await RetryAsyncBasic<TResult>(taskProvider, source, requestId, maxRetries, retryDelayInMs);
        }


        private static async Task<TResult> RetryAsyncBasic<TResult>(dynamic taskProvider, string source = "", string requestId = "", int maxRetries = 3, int retryDelayInMs = 500, object funcParam = null)
        {
            int retryCount = 0;
            DateTime taskInvocationStartTime = DateTime.UtcNow;
            Exception attemptException = null;
            var exceptions = new List<Exception>();
            TResult taskProviderResult = default(TResult);
            Task<TResult> taskProviderTask = null;

            do
            {
                try
                {
                    DiagnosticsETWProvider.Instance.LogRetryAttemptMessage(
                        requestId ?? string.Empty,
                        source ?? string.Empty,
                        $"Starting Retry Attempt : {retryCount}"
                        );

                    taskInvocationStartTime = DateTime.UtcNow;
                    attemptException = null;

                    if (funcParam == null)
                    {
                        taskProviderTask = taskProvider();
                    }
                    else
                    {
                        taskProviderTask = taskProvider(funcParam);
                    }
                    taskProviderResult = await taskProviderTask;

                    break;
                }
                catch (Exception ex)
                {
                    attemptException = ex;
                    exceptions.Add(ex);
                }
                finally
                {
                    if (retryCount > 0)
                    {
                        string exceptionType = attemptException != null ? attemptException.GetType().ToString() : string.Empty;
                        string exceptionDetails = attemptException != null ? attemptException.ToString() : string.Empty;
                        DateTime taskInvocationEndTime = DateTime.UtcNow;
                        long latencyInMs = Convert.ToInt64((taskInvocationEndTime - taskInvocationStartTime).TotalMilliseconds);

                        DiagnosticsETWProvider.Instance.LogRetryAttemptSummary(
                            requestId ?? string.Empty,
                            source ?? string.Empty,
                            $"Retry Attempt : {retryCount}, IsSuccessful : {attemptException == null}",
                            latencyInMs,
                            taskInvocationStartTime.ToString("HH:mm:ss.fff"),
                            taskInvocationEndTime.ToString("HH:mm:ss.fff"),
                            exceptionType,
                            exceptionDetails
                            );
                    }
                    retryCount++;
                }

                if (retryCount < maxRetries) await Task.Delay(retryDelayInMs);
            } while (retryCount < maxRetries);

            if (taskProviderTask != null && taskProviderTask.IsCompleted && !taskProviderTask.IsFaulted && !taskProviderTask.IsCanceled)
            {
                return taskProviderResult;
            }

            throw new AggregateException($"Failed {maxRetries} retries. Look at inner exceptions", exceptions);
        }
    }
}
