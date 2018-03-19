using System.Threading.Tasks;

namespace Diagnostics.Scripts.CompilationService.Interfaces
{
    public interface ICompilationService
    {
        Task<ICompilation> GetCompilationAsync();
    }
}
