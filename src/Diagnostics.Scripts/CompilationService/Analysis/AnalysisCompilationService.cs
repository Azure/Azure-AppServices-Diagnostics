using System.Threading.Tasks;
using Diagnostics.Scripts.CompilationService.Interfaces;
using Diagnostics.Scripts.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace Diagnostics.Scripts.CompilationService
{
    public class AnalysisCompilationService : CompilationServiceBase
    {
        public AnalysisCompilationService(EntityMetadata entityMetadata, ScriptOptions scriptOptions) : base(entityMetadata, scriptOptions)
        {
        }

        protected override Task<ICompilation> CreateCompilationObject(Compilation scriptCompilation) => Task.FromResult<ICompilation>(new AnalysisCompilation(scriptCompilation));
    }
}
