using Diagnostics.Scripts.CompilationService.Interfaces;
using Diagnostics.Scripts.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Threading.Tasks;

namespace Diagnostics.Scripts.CompilationService
{
    public abstract class CompilationServiceBase : ICompilationService
    {
        private EntityMetadata _entityMetadata;

        private ScriptOptions _scriptOptions;

        public CompilationServiceBase(EntityMetadata entityMetadata, ScriptOptions scriptOptions)
        {
            _entityMetadata = entityMetadata;
            _scriptOptions = scriptOptions;
        }

        public Task<ICompilation> GetCompilationAsync()
        {
            Script<object> script = CSharpScript.Create<object>(_entityMetadata.ScriptText, _scriptOptions);
            return CreateCompilationObject(GetScriptCompilation(script));
        }

        private Compilation GetScriptCompilation(Script<object> script)
        {
            return script.GetCompilation();
        }

        protected abstract Task<ICompilation> CreateCompilationObject(Compilation scriptCompilation);
    }
}
