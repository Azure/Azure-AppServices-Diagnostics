using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils
{
    public class CompilerResponse
    {
        public bool CompilationSucceeded;

        public IEnumerable<string> CompilationOutput;

        public string AssemblyBytes;

        public string PdbBytes;
    }
}
