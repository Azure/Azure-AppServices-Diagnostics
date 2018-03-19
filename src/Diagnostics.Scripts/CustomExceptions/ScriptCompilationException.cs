using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Diagnostics.Scripts
{
    public class ScriptCompilationException : Exception
    {
        public IEnumerable<string> CompilationOutput;

        public ScriptCompilationException()
        {
            CompilationOutput = Enumerable.Empty<string>();
        }

        public ScriptCompilationException(IEnumerable<string> compilationErrors)
            : base("script compliation failed.")
        {
            CompilationOutput = compilationErrors;
        }

        public ScriptCompilationException(string message)
        : base(message)
        {
            CompilationOutput = Enumerable.Empty<string>();
            CompilationOutput = CompilationOutput.Concat(new string[] { message });
        }

        public ScriptCompilationException(string message, Exception inner)
            : base(message, inner)
        {
            CompilationOutput = Enumerable.Empty<string>();
            CompilationOutput = CompilationOutput.Concat(new string[] { message });
        }
    }
}
