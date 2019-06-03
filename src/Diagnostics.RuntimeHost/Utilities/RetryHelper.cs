using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Diagnostics.Logger;

namespace Diagnostics.RuntimeHost.Utilities
{
    internal class RetryHelper
    {
        internal static async Task<TResult> RetryAsync<TResult>(Func<Task<TResult>> taskProvider, string source = "", string requestId = "", int maxRetries = 3, int retryDelayInMs = 500)
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

                    taskProviderTask = taskProvider();
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

                    retryCount++;
                }

                if (retryCount < maxRetries) await Task.Delay(retryDelayInMs);
            } while (retryCount < maxRetries);

            if (taskProviderTask.IsCompleted && !taskProviderTask.IsFaulted && !taskProviderTask.IsCanceled)
            {
                return taskProviderResult;
            }

            throw new AggregateException($"Failed {maxRetries} retries. Look at inner exceptions", exceptions);
        }
    }
}
