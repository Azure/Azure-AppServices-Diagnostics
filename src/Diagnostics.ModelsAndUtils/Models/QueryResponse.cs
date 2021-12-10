using System.Collections.Generic;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class QueryResponse<T>
    {
        public CompilerResponse CompilationOutput { get; set; }
        public IEnumerable<RuntimeLogEntry> RuntimeLogOutput { get; set; }

        public bool RuntimeSucceeded { get; set; }

        public T InvocationOutput { get; set; }
    }
}
