using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class RuntimeLogScope
    {
        public string Text { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }

    public class RuntimeLogEntry
    {
        public DateTime TimeStamp { get; private set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public int EventId { get; set; }
        public object State { get; set; }
        public string StateText { get; set; }
        public Dictionary<string, object> StateProperties { get; set; }
        public List<RuntimeLogScope> Scopes { get; set; }

        public RuntimeLogEntry()
        {
            this.TimeStamp = DateTime.UtcNow;
        }
    }
}
