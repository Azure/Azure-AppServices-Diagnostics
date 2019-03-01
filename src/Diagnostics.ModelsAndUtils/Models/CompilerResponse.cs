using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class CompilerResponse
    {
        public bool CompilationSucceeded;

        public IEnumerable<string> CompilationTraces;

        public IEnumerable<string> References;

        public string AssemblyBytes;

        public string PdbBytes;

        public string AssemblyName;
    }
}
