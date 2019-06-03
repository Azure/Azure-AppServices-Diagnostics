// <copyright file="ETWMessageTemplates.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System.Diagnostics.CodeAnalysis;

namespace Diagnostics.Logger
{
    /// <summary>
    /// ETW message template.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Message templates are enough explanation.")]
    public static class ETWMessageTemplates
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        #region Compile Host Event Message Templates

        public const string LogCompilerHostMessage = "Compiler Host Message : {0}";
        public const string LogCompilerHostUnhandledException = "Compiler Host Unhandled Exception : {3}";
        public const string LogCompilerHostAPISummary = "Compiler Host API Response Code : {3}";

        #endregion Compile Host Event Message Templates

        #region Runtime Host Event Message Templates

        public const string LogRuntimeHostMessage = "Runtime Host Message : {0}";
        public const string LogRuntimeHostUnhandledException = "Runtime Host Unhandled Exception : {3}";
        public const string LogRuntimeHostAPISummary = "Runtime Host API Response Code : {3}";
        public const string LogRetryAttemptSummary = "Retry Attempt Summary";
        public const string LogRetryAttemptMessage = "Retry Attempt Message";
        public const string LogRuntimeHostInsightsCorrelation = "Insights Correlation Id";
        public const string LogRuntimeHostHandledException = "Runtime Host Handled Exception : {3}";
        public const string LogRuntimeHostDetectorAscInsight = "ASC Insight Detail for Detector";
        public const string LogRuntimeHostSupportTopicAscInsight = "ASC Insight Detail for Detector";

        #endregion Runtime Host Event Message Templates

        #region Source Watcher Event Message Templates

        public const string LogSourceWatcherMessage = "Source Watcher : {0},  Message : {1}";
        public const string LogSourceWatcherWarning = "Source Watcher : {0},  Warning : {1}";
        public const string LogSourceWatcherException = "Source Watcher : {0},  Exception : {3}";

        #endregion Source Watcher Event Message Templates

        #region Compiler Host Client Event Message Templates

        public const string LogCompilerHostClientMessage = "Compiler Host Client Message : {2}";
        public const string LogCompilerHostClientException = "Compiler Host Client Exception : {4}";
        public const string LogCompilerHostClientWarning = "Compiler Host Client Warnings";

        #endregion Compiler Host Client Event Message Templates

        #region Data Provider Event Message Templates

        public const string LogDataProviderMessage = "Data Provider Informational Details. Source : {1} Message : {2}";
        public const string LogDataProviderException = "An exception occurred in Source : {1}. ExceptionType : {5} ExceptionDetails : {6}";
        public const string LogDataProviderOperationSummary = "Data Provider Operation. Source : {1} StartTime : {2} EndTime : {3} LatencyInMilliseconds : {4}";
        public const string LogTokenRefreshSummary = "Token Refresh Summary";
        public const string LogKustoQueryInformation = "Kusto Query Information";
        public const string LogKustoHeartbeatInformation = "Kusto Heart Beat Information";

        #endregion Data Provider Event Message Templates

        #region Internal AI API Event Message Templates

        public const string LogInternalAPIMessage = "Internal API Message";
        public const string LogInternalAPIInsights = "Internal API Insights";
        public const string LogInternalAPISummary = "Internal API Response Code";
        public const string LogInternalAPIHandledException = "Internal API Handled Exception";
        public const string LogInternalAPIUnhandledException = "Internal API Unhandled Exception";
        public const string LogInternalAPITrainingException = "Internal API Training Exception";
        public const string LogInternalAPITrainingSummary = "Internal API Training Summary";

        #endregion Internal AI API Event Message Templates

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
