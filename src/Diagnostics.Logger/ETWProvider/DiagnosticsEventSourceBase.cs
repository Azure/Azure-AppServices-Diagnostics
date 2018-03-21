using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace Diagnostics.Logger
{
    public abstract class DiagnosticsEventSourceBase : EventSource
    {
        [NonEvent]
        protected void WriteDiagnosticsEvent(int eventId, params object[] args)
        {
            WriteEvent(eventId, args);
        }
    }
}
