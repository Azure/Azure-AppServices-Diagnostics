using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
            if (context.Request.Headers.TryGetValue(HeaderConstants.RequestIdHeaderName, out StringValues values) && values != default(StringValues) && values.Any())
            {
                _requestId = values.First().ToString();
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

                Regex webAppRegEx = new Regex("(.*)subscriptions/(.*)/resourcegroups/(.*)/providers/microsoft.web/sites/(.*)/diagnostics/(.*)");
                Regex hostingEnvRegEx = new Regex("(.*)subscriptions/(.*)/resourcegroups/(.*)/providers/microsoft.web/hostingenvironments/(.*)/diagnostics/(.*)");

                string addressPath = httpContext.Request.Path.ToString().ToLower();
                Match webAppMatch = webAppRegEx.Match(addressPath);

                if (webAppMatch.Success)
                {
                    _subscriptionId = webAppMatch.Groups[2].Value;
                    _resourceGroupName = webAppMatch.Groups[3].Value;
                    _resourceName = webAppMatch.Groups[4].Value;
                }
                else
                {
                    Match hostingEnvMatch = hostingEnvRegEx.Match(addressPath);
                    if (hostingEnvMatch.Success)
                    {
                        _subscriptionId = hostingEnvMatch.Groups[2].Value;
                        _resourceGroupName = hostingEnvMatch.Groups[3].Value;
                        _resourceName = hostingEnvMatch.Groups[4].Value;
                    }
                }
            }
            catch (Exception)
            {
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
