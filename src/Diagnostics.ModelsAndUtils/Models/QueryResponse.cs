using System.Collections.Generic;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class QueryResponse<T>
    {
        public CompilerResponse CompilationOutput;
        public IEnumerable<RuntimeEventLogEntry> RuntimeOutput;

        public bool RuntimeSucceeded;

        public T InvocationOutput;
    }
}
