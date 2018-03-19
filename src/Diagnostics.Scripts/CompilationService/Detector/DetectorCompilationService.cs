using Diagnostics.Scripts.CompilationService.Interfaces;
using Diagnostics.Scripts.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System.Threading.Tasks;

namespace Diagnostics.Scripts.CompilationService
{
    public class DetectorCompilationService : CompilationServiceBase
    {
        public DetectorCompilationService(EntityMetadata entityMetadata, ScriptOptions scriptOptions) : base(entityMetadata, scriptOptions)
        {
        }

        protected override Task<ICompilation> CreateCompilationObject(Compilation scriptCompilation) => Task.FromResult<ICompilation>(new DetectorCompilation(scriptCompilation));
    }
}
