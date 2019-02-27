// <copyright file="DiagnosticsEventSourceBase.cs" company="Microsoft">
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>

using System.Diagnostics.Tracing;

namespace Diagnostics.Logger
{
    /// <summary>
    /// Diagnostics event source base.
    /// </summary>
    public abstract class DiagnosticsEventSourceBase : EventSource
    {
        /// <summary>
        /// Write diagnostics event.
        /// </summary>
        /// <param name="eventId">Event id.</param>
        /// <param name="args">The args.</param>
        [NonEvent]
        protected void WriteDiagnosticsEvent(int eventId, params object[] args)
        {
            WriteEvent(eventId, args);
        }
    }
}
