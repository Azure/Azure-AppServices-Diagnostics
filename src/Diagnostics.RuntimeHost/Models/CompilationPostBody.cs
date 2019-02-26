using Diagnostics.RuntimeHost.Utilities;
using System.Collections.Generic;

namespace Diagnostics.RuntimeHost.Models
{
    /// <summary>
    /// Compilation post body
    /// </summary>
    /// <typeparam name="T">Resource type.</typeparam>
    public class CompilationPostBody<T>
    {
        private string _sanitizedScript;

        /// <summary>
        /// Gets or sets the script.
        /// </summary>
        public string Script
        {
            get
            {
                return _sanitizedScript;
            }
            set
            {
                _sanitizedScript = FileHelper.SanitizeScriptFile(value);
            }
        }

        /// <summary>
        /// Gets or sets the source code reference.
        /// </summary>
        public IDictionary<string, string> References { get; set; }

        /// <summary>
        /// Gets or sets the resource.
        /// </summary>
        public T Resource { get; set; }
    }
}
