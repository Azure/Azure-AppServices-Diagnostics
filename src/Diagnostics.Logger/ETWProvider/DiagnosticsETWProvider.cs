using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace Diagnostics.Logger
{
    [EventSource(Name = "Microsoft-Azure-AppService-Diagnostics")]
    public sealed class DiagnosticsETWProvider : DiagnosticsEventSourceBase
    {
        public static DiagnosticsETWProvider Instance = new DiagnosticsETWProvider();

        #region Compile Host Events (ID Range : 1000 - 1999)

        [Event(1000, Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void LogCompilerHostStartup(string Message)
        {
            WriteDiagnosticsEvent(1000, Message);
        }

        [Event(1001, Level = EventLevel.Error, Channel = EventChannel.Admin)]
        public void LogCompilerHostUnhandledException(string RequestId, string Source, string ExceptionType, string ExceptionDetails)
        {
            WriteDiagnosticsEvent(1001,
                RequestId,
                Source,
                ExceptionType,
                ExceptionDetails);
        }

        [Event(1002, Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void LogCompilerHostAPISummary(string RequestId, string Address, string Verb, int StatusCode, long LatencyInMilliseconds, string StartTime, string EndTime)
        {
            WriteDiagnosticsEvent(1002,
                RequestId,
                Address,
                Verb,
                StatusCode,
                LatencyInMilliseconds,
                StartTime,
                EndTime);
        }

        #endregion

        #region Runtime Host Events (ID Range : 2000 - 2999)

        [Event(2000, Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void LogRuntimeHostStartup(string Message)
        {
            WriteDiagnosticsEvent(2000, Message);
        }

        [Event(2001, Level = EventLevel.Error, Channel = EventChannel.Admin)]
        public void LogRuntimeHostUnhandledException(string RequestId, string Source, string SubscriptionId, string ResourceGroup, string Resource, string ExceptionType, string ExceptionDetails)
        {
            WriteDiagnosticsEvent(2001,
                RequestId,
                Source,
                SubscriptionId,
                ResourceGroup,
                Resource,
                ExceptionType,
                ExceptionDetails);
        }

        [Event(2002, Level = EventLevel.Informational, Channel = EventChannel.Admin)]
        public void LogRuntimeHostAPISummary(string RequestId, string SubscriptionId, string ResourceGroup, string Resource, string Address, string Verb, string OperationName, int StatusCode, long LatencyInMilliseconds, string StartTime, string EndTime)
        {
            WriteDiagnosticsEvent(2002,
                RequestId,
                SubscriptionId,
                ResourceGroup,
                Resource,
                Address,
                Verb,
                OperationName,
                StatusCode,
                LatencyInMilliseconds,
                StartTime,
                EndTime);
        }

        #endregion

        #region Data Provider Events (ID Range : 3000 - 3999)
        #endregion
    }
}
