using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;
using Diagnostics.Scripts.Models;
using Microsoft.CodeAnalysis;

namespace Diagnostics.Scripts.CompilationService.Interfaces
{
    public interface ICompilation
    {
        Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync();

        EntityMethodSignature GetEntryPointSignature();

        Task<MemberInfo> GetEntryPoint();

        MemberInfo GetEntryPoint(Assembly assembly);

        Task<Assembly> EmitAssemblyAsync();

        Task<string> SaveAssemblyAsync(string assemblyPath);

        Task<Tuple<string, string>> GetAssemblyBytesAsync();
    }
}
