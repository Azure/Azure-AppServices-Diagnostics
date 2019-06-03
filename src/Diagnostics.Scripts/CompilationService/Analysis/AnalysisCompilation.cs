using System.Collections.Immutable;
using Diagnostics.Scripts.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Diagnostics.Scripts.CompilationService
{
    public class AnalysisCompilation : CompilationBase
    {
        private static string _entryPointMethodName = "Run";

        public AnalysisCompilation() : base(EntryPointResolutionType.MethodName, _entryPointMethodName)
        {
        }

        public AnalysisCompilation(Compilation compilation) : base(compilation, EntryPointResolutionType.MethodName, _entryPointMethodName)
        {
        }

        protected override ImmutableArray<DiagnosticAnalyzer> GetCodeAnalyzers()
        {
            return ImmutableArray.Create<DiagnosticAnalyzer>();
        }
    }
}
