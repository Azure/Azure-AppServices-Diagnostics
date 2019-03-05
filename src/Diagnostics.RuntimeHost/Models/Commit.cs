using System;
using System.Collections.Immutable;

namespace Diagnostics.RuntimeHost.Models
{
    /// <summary>
    /// Class for github commit.
    /// </summary>
    public class Commit
    {
        /// <summary>
        /// Gets or sets the message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the commit content.
        /// </summary>
        public ImmutableDictionary<string, Tuple<string, bool>> Content { get; set; }
    }
}
