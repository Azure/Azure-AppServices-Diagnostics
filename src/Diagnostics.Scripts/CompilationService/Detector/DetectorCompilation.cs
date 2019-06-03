using System.Collections.Immutable;
using Diagnostics.Scripts.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Diagnostics.Scripts.CompilationService
{
    public class DetectorCompilation : CompilationBase
    {
        private static string _entryPointMethodName = "Run";

        public DetectorCompilation() : base(EntryPointResolutionType.MethodName, _entryPointMethodName)
        {
        }

        public DetectorCompilation(Compilation compilation) : base(compilation, EntryPointResolutionType.MethodName, _entryPointMethodName)
        {
        }

        protected override ImmutableArray<DiagnosticAnalyzer> GetCodeAnalyzers()
        {
            return ImmutableArray.Create<DiagnosticAnalyzer>();
        }
    }
}
