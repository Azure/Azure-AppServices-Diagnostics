using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.Logger
{
    public class ApiMetricsLogger
    {
        private string _requestId;
        private string _address;
        private string _verb;
        private DateTime _startTime;
        private DateTime _endTime;
        private long _latencyInMs;
        private string _subscriptionId;
        private string _resourceGroupName;
        private string _resourceName;

        public ApiMetricsLogger(HttpContext context)
        {
            StartMetricCapture(context);
        }

        private void StartMetricCapture(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues values))
            {
                _requestId = values.ToString();
            }

            _startTime = DateTime.UtcNow;
            _verb = context.Request.Method;
            _address = context.Request.Path.ToString();

            ParseFromAddress(context);
        }

        private void ParseFromAddress(HttpContext httpContext)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(httpContext.Request.Path.ToString()))
                {
                    return;
                }

                var parts = httpContext.Request.Path.ToString().ToLowerInvariant().Split('?')[0].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var cursor = ((IEnumerable<string>)parts).GetEnumerator();
                while (cursor.MoveNext())
                {
                    if (String.Equals(cursor.Current, "subscriptions", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }

                if (!cursor.MoveNext())
                {
                    return;
                }

                _subscriptionId = cursor.Current;

                if (!cursor.MoveNext())
                {
                    return;
                }

                if (String.Equals(cursor.Current, "resourceGroups", StringComparison.OrdinalIgnoreCase))
                {
                    if (!cursor.MoveNext())
                    {
                        return;
                    }

                    _resourceGroupName = cursor.Current;

                    if (!cursor.MoveNext() || !String.Equals(cursor.Current, "providers", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                    if (!cursor.MoveNext() || !String.Equals(cursor.Current, "Microsoft.Web", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }

                if (cursor.MoveNext() && String.Equals(cursor.Current, "sites", StringComparison.OrdinalIgnoreCase) || String.Equals(cursor.Current, "hostingEnvironments", StringComparison.OrdinalIgnoreCase))
                {
                    if (!cursor.MoveNext())
                    {
                        return;
                    }

                    _resourceName = cursor.Current;
                }
                else
                {
                    return;
                }
            }
            catch
            {
                // ignore error
            }
        }

        public void LogRuntimeHostAPIMetrics(HttpContext context)
        {
            if (context == null) return;

            _endTime = DateTime.UtcNow;
            _latencyInMs = Convert.ToInt64((_endTime - _startTime).TotalMilliseconds);

            DiagnosticsETWProvider.Instance.LogRuntimeHostAPISummary(
                _requestId ?? string.Empty,
                _subscriptionId ?? string.Empty,
                _resourceGroupName ?? string.Empty,
                _resourceName ?? string.Empty,
                _address ?? string.Empty,
                _verb ?? string.Empty,
                string.Empty,
                context.Response.StatusCode,
                _latencyInMs,
                _startTime.ToString("HH:mm:ss.fff"),
                _endTime.ToString("HH:mm:ss.fff")
                );
        }

        public void LogCompilerHostAPIMetrics(HttpContext context)
        {
            if (context == null) return;

            _endTime = DateTime.UtcNow;
            _latencyInMs = Convert.ToInt64((_endTime - _startTime).TotalMilliseconds);

            DiagnosticsETWProvider.Instance.LogCompilerHostAPISummary(
                _requestId ?? string.Empty,
                _address ?? string.Empty,
                _verb ?? string.Empty,
                context.Response.StatusCode,
                _latencyInMs,
                _startTime.ToString("HH:mm:ss.fff"),
                _endTime.ToString("HH:mm:ss.fff")
                );
        }

        public void LogRuntimeHostUnhandledException(HttpContext context, Exception ex)
        {
            if (context == null)
            {
                return;
            }

            DiagnosticsETWProvider.Instance.LogRuntimeHostUnhandledException(
                _requestId ?? string.Empty,
                _address ?? string.Empty,
                _subscriptionId ?? string.Empty,
                _resourceGroupName ?? string.Empty,
                _resourceName ?? string.Empty,
                ex != null ? ex.GetType().ToString() : string.Empty,
                ex != null ? ex.ToString() : string.Empty
            );
        }

        public void LogCompilerHostUnhandledException(HttpContext context, Exception ex)
        {
            if (context == null)
            {
                return;
            }

            DiagnosticsETWProvider.Instance.LogCompilerHostUnhandledException(
                _requestId ?? string.Empty,
                _address ?? string.Empty,
                ex != null ? ex.GetType().ToString() : string.Empty,
                ex != null ? ex.ToString() : string.Empty
            );
        }
    }
}
