using Diagnostics.Scripts.CompilationService.Interfaces;
using Diagnostics.Scripts.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Diagnostics.Scripts.CompilationService
{
    public abstract class CompilationBase : ICompilation
    {
        private readonly EntryPointResolutionType _resolutionType;
        private readonly string _entryPointName;
        protected readonly Compilation _compilation;

        protected abstract ImmutableArray<DiagnosticAnalyzer> GetCodeAnalyzers();

        public CompilationBase(Compilation compilation)
        {
            _compilation = compilation;
        }

        public CompilationBase(EntryPointResolutionType resolutionType, string entryPointName)
        {
            _resolutionType = resolutionType;
            _entryPointName = entryPointName;
        }

        public CompilationBase(Compilation compilation, EntryPointResolutionType resolutionType, string entryPointName)
        {
            _compilation = compilation;
            _resolutionType = resolutionType;
            _entryPointName = entryPointName;
        }

        public Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync()
        {
            if(_compilation == null)
            {
                throw new ArgumentException("Compilation Object not initialized.");
            }

            ImmutableArray<DiagnosticAnalyzer> analyzers = GetCodeAnalyzers();
            if (analyzers.IsEmpty)
            {
                return Task.Factory.StartNew(() => _compilation.GetDiagnostics());
            }

            return _compilation.WithAnalyzers(GetCodeAnalyzers()).GetAllDiagnosticsAsync();
        }

        public EntityMethodSignature GetEntryPointSignature()
        {
            if (_compilation == null)
            {
                return new EntityMethodSignature(_entryPointName);
            }

            var methods = _compilation.ScriptClass
                .GetMembers()
                .OfType<IMethodSymbol>();

            IMethodSymbol entryPointReference = default(IMethodSymbol);

            switch (_resolutionType)
            {
                case EntryPointResolutionType.Attribute:
                    break;
                case EntryPointResolutionType.MethodName:
                default:
                    entryPointReference = GetMethodByName(methods, _entryPointName);
                    break;
            }

            if (entryPointReference == default(IMethodSymbol))
            {
                throw new ScriptCompilationException($"No Entry point found. Entry point resoultion type : {_resolutionType} , value : {_entryPointName}");
            }

            var methodParameters = entryPointReference.Parameters.Select(p => new EntityParameter(p.Name, GetFullTypeName(p.Type), p.IsOptional, p.RefKind));
            var attributes = entryPointReference.GetAttributes();
            return new EntityMethodSignature(
                entryPointReference.ContainingType.ToDisplayString(),
                entryPointReference.Name,
                ImmutableArray.CreateRange(methodParameters.ToArray()),
                GetFullTypeName(entryPointReference.ReturnType),
                attributes);
        }

        public virtual MemberInfo GetEntryPoint(Assembly assembly)
        {
            if (_compilation == null)
            {
                return new EntityMethodSignature(_entryPointName).GetMethod(assembly);
            }

            var methods = _compilation.ScriptClass
                .GetMembers()
                .OfType<IMethodSymbol>();

            IMethodSymbol entryPointReference = default(IMethodSymbol);

            switch (_resolutionType)
            {
                case EntryPointResolutionType.Attribute:
                    break;
                case EntryPointResolutionType.MethodName:
                default:
                    entryPointReference = GetMethodByName(methods, _entryPointName);
                    break;
            }

            if (entryPointReference == default(IMethodSymbol))
            {
                throw new ScriptCompilationException($"No Entry point found. Entry point resoultion type : {_resolutionType} , value : {_entryPointName}");
            }

            var methodParameters = entryPointReference.Parameters.Select(p => new EntityParameter(p.Name, GetFullTypeName(p.Type), p.IsOptional, p.RefKind));
            var attributes = entryPointReference.GetAttributes();
            return new EntityMethodSignature(
                entryPointReference.ContainingType.ToDisplayString(),
                entryPointReference.Name,
                ImmutableArray.CreateRange(methodParameters.ToArray()),
                GetFullTypeName(entryPointReference.ReturnType),
                attributes).GetMethod(assembly);
        }

        public async Task<MemberInfo> GetEntryPoint()
        {
            var assembly = await EmitAssemblyAsync();

            return GetEntryPoint(assembly);
        }

        public Task<Assembly> EmitAssemblyAsync()
        {
            if (_compilation == null)
            {
                throw new ArgumentException("Compilation Object not initialized.");
            }

            return Task.Factory.StartNew<Assembly>(() =>
            {
                try
                {
                    using (var assemblyStream = new MemoryStream())
                    using (var pdbStream = new MemoryStream())
                    {
                        _compilation.Emit(assemblyStream, pdbStream);
                        return Assembly.Load(assemblyStream.GetBuffer(), pdbStream.GetBuffer());
                    }
                }
                catch (Exception)
                {
                    // TODO : Need to throw custom exception?
                    throw;
                }
            });
        }

        public async Task<string> SaveAssemblyAsync(string assemblyPath)
        {
            if (_compilation == null)
            {
                throw new ArgumentException("Compilation Object not initialized.");
            }

            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentNullException("assemblyPath");
            }

            string pdbPath = assemblyPath;
            if (!assemblyPath.EndsWith(".dll"))
            {
                assemblyPath = $"{assemblyPath}.dll";
                pdbPath = $"{pdbPath}.pdb";
            }
            else
            {
                pdbPath = assemblyPath.Replace(".dll", ".pdb");
            }


            if (File.Exists(assemblyPath))
            {
                throw new IOException($"Assembly File already exists : {assemblyPath}");
            }

            using (var assemblyStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                _compilation.Emit(assemblyStream, pdbStream);

                using (FileStream asmFs = File.Create(assemblyPath))
                using (FileStream pdbFs = File.Create(pdbPath))
                {
                    assemblyStream.Position = 0;
                    pdbStream.Position = 0;
                    await assemblyStream.CopyToAsync(asmFs);
                    await pdbStream.CopyToAsync(pdbFs);
                }
            }

            return assemblyPath;
        }

        public Task<Tuple<string, string>> GetAssemblyBytesAsync()
        {
            if (_compilation == null)
            {
                throw new ArgumentException("Compilation Object not initialized.");
            }

            return Task.Factory.StartNew(() =>
            {
                Tuple<string, string> asmEncodedBytes;
                try
                {
                    using (var assemblyStream = new MemoryStream())
                    using (var pdbStream = new MemoryStream())
                    {
                        _compilation.Emit(assemblyStream, pdbStream);
                        asmEncodedBytes = new Tuple<string, string>(
                            Convert.ToBase64String(assemblyStream.GetBuffer()),
                            Convert.ToBase64String(pdbStream.GetBuffer()));

                        return asmEncodedBytes;
                    }
                }
                catch (Exception)
                {
                    // TODO : Need to throw custom exception?
                    throw;
                }
            });
        }

        private IMethodSymbol GetMethodByName(IEnumerable<IMethodSymbol> methods, string methodName)
        {
            var namedMethods = methods
                       .Where(m => m.DeclaredAccessibility == Accessibility.Public && string.Compare(m.Name, methodName, StringComparison.Ordinal) == 0)
                       .ToList();

            if (namedMethods.Count == 1)
            {
                return namedMethods.First();
            }

            // If we have multiple public methods matching the provided name, throw a compilation exception
            if (namedMethods.Count > 1)
            {
                throw new ScriptCompilationException($"Multiple Entry Point Methods with name {methodName} found.");
            }

            return default(IMethodSymbol);
        }

        private string GetFullTypeName(ITypeSymbol type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            return type.ContainingAssembly == null
                ? type.ToDisplayString()
                : string.Format(CultureInfo.InvariantCulture, "{0}, {1}", type.ToDisplayString(), type.ContainingAssembly.ToDisplayString());
        }

        
    }
}
