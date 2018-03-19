using Diagnostics.Scripts.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Diagnostics.Scripts.CompilationService
{
    public class SignalCompilation : CompilationBase
    {
        private static string _entryPointMethodName = "Run";

        public SignalCompilation() : base(EntryPointResolutionType.MethodName, _entryPointMethodName)
        {
        }

        public SignalCompilation(Compilation compilation) : base(compilation, EntryPointResolutionType.MethodName, _entryPointMethodName)
        {
        }

        protected override ImmutableArray<DiagnosticAnalyzer> GetCodeAnalyzers()
        {
            return ImmutableArray.Create<DiagnosticAnalyzer>();
        }
    }
}
