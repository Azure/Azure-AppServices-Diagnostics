using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.Logger
{
    public static class ETWMessageTemplates
    {
        #region Compile Host Event Message Templates

        public const string LogCompilerHostMessage = "Compiler Host Message : {0}";
        public const string LogCompilerHostUnhandledException = "Compiler Host Unhandled Exception : {3}";
        public const string LogCompilerHostAPISummary = "Compiler Host API Response Code : {3}";

        #endregion

        #region Runtime Host Event Message Templates

        public const string LogRuntimeHostMessage = "Runtime Host Message : {0}";
        public const string LogRuntimeHostUnhandledException = "Runtime Host Unhandled Exception : {3}";
        public const string LogRuntimeHostAPISummary = "Runtime Host API Response Code : {3}";
        public const string LogRetryAttemptSummary = "Retry Attempt Summary";
        public const string LogRetryAttemptMessage = "Retry Attempt Message";
        public const string LogRuntimeHostInsightsCorrelation = "Insights Correlation Id";
        public const string LogRuntimeHostHandledException = "Runtime Host Handled Exception : {3}";
        public const string LogRuntimeHostDetectorAscInsight = "ASC Insight Detail for Detector";

        #endregion

        #region Source Watcher Event Message Templates

        public const string LogSourceWatcherMessage = "Source Watcher : {0},  Message : {1}";
        public const string LogSourceWatcherWarning = "Source Watcher : {0},  Warning : {1}";
        public const string LogSourceWatcherException = "Source Watcher : {0},  Exception : {3}";

        #endregion

        #region Compiler Host Client Event Message Templates

        public const string LogCompilerHostClientMessage = "Compiler Host Client Message : {2}";
        public const string LogCompilerHostClientException = "Compiler Host Client Exception : {4}";
        public const string LogCompilerHostClientWarning = "Compiler Host Client Warnings";

        #endregion

        #region Data Provider Event Message Templates
        public const string LogDataProviderMessage = "Data Provider Informational Details. Source : {1} Message : {2}";
        public const string LogDataProviderException = "An exception occurred in Source : {1}. ExceptionType : {5} ExceptionDetails : {6}";
        public const string LogDataProviderOperationSummary = "Data Provider Operation. Source : {1} StartTime : {2} EndTime : {3} LatencyInMilliseconds : {4}";
        #endregion
    }
}
