// <copyright file="EntityInvoker.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.Scripts.CompilationService;
using Diagnostics.Scripts.CompilationService.Gist;
using Diagnostics.Scripts.CompilationService.Interfaces;
using Diagnostics.Scripts.CompilationService.ReferenceResolver;
using Diagnostics.Scripts.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace Diagnostics.Scripts
{
    /// <summary>
    /// Class for entity invoker.
    /// </summary>
    public sealed class EntityInvoker : IDisposable
    {
        private EntityMetadata _entityMetaData;
        private ImmutableArray<string> _frameworkReferences;
        private ImmutableArray<string> _frameworkImports;
        private ImmutableDictionary<string, string> _frameworkLoads;
        private ICompilation _compilation;
        private ImmutableArray<Diagnostic> _diagnostics;
        private MemberInfo memberInfo;
        private Definition _entryPointDefinitionAttribute;
        private IResourceFilter _resourceFilter;
        private SystemFilter _systemFilter;

        public IEnumerable<string> References { get; private set; }

        public bool IsCompilationSuccessful { get; private set; }

        public IEnumerable<string> CompilationOutput { get; private set; }

        public EntityMetadata EntityMetadata => _entityMetaData;

        public Definition EntryPointDefinitionAttribute => _entryPointDefinitionAttribute;

        public IResourceFilter ResourceFilter => _resourceFilter;

        public SystemFilter SystemFilter => _systemFilter;

        public EntityInvoker(EntityMetadata entityMetadata)
        {
            _entityMetaData = entityMetadata;
            _frameworkReferences = ImmutableArray<string>.Empty;
            _frameworkImports = ImmutableArray<string>.Empty;
            _frameworkLoads = ImmutableDictionary<string, string>.Empty;
            CompilationOutput = Enumerable.Empty<string>();
        }

        public EntityInvoker(EntityMetadata entityMetadata, ImmutableArray<string> frameworkReferences)
        {
            _entityMetaData = entityMetadata;
            _frameworkReferences = frameworkReferences;
            _frameworkImports = ImmutableArray<string>.Empty;
            _frameworkLoads = ImmutableDictionary<string, string>.Empty;
            CompilationOutput = Enumerable.Empty<string>();
        }

        public EntityInvoker(EntityMetadata entityMetadata, ImmutableArray<string> frameworkReferences, ImmutableArray<string> frameworkImports)
        {
            _entityMetaData = entityMetadata;
            _frameworkImports = frameworkImports;
            _frameworkReferences = frameworkReferences;
            _frameworkLoads = ImmutableDictionary<string, string>.Empty;
            CompilationOutput = Enumerable.Empty<string>();
        }

        public EntityInvoker(EntityMetadata entityMetadata, ImmutableArray<string> frameworkReferences, ImmutableArray<string> frameworkImports, ImmutableDictionary<string, string> frameworkLoads)
        {
            _entityMetaData = entityMetadata;
            _frameworkImports = frameworkImports;
            _frameworkReferences = frameworkReferences;
            _frameworkLoads = frameworkLoads;
            CompilationOutput = Enumerable.Empty<string>();
        }

        /// <summary>
        /// Initializes the entry point by compiling the script and loading/saving the assembly
        /// </summary>
        /// <returns>Taks for initializing entry point.</returns>
        public async Task InitializeEntryPointAsync()
        {
            var referenceResolver = new MemoryReferenceResolver(_frameworkLoads);
            ICompilationService compilationService = CompilationServiceFactory.CreateService(_entityMetaData, GetScriptOptions(referenceResolver));
            _compilation = await compilationService.GetCompilationAsync();
            _diagnostics = await _compilation.GetDiagnosticsAsync();
            References = referenceResolver.Used.ToImmutableArray();

            IsCompilationSuccessful = !_diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
            CompilationOutput = _diagnostics.Select(m => m.ToString());

            if (IsCompilationSuccessful)
            {
                try
                {
                    memberInfo = await _compilation.GetEntryPoint();
                    InitializeAttributes();
                    Validate();
                }
                catch (Exception ex)
                {
                    if (ex is ScriptCompilationException scriptEx)
                    {
                        IsCompilationSuccessful = false;

                        if (scriptEx.CompilationOutput.Any())
                        {
                            CompilationOutput = CompilationOutput.Concat(scriptEx.CompilationOutput);
                        }

                        return;
                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// Initializes the entry point from already loaded assembly.
        /// </summary>
        /// <param name="asm">The assembly.</param>
        public void InitializeEntryPoint(Assembly asm)
        {
            if (asm == null)
            {
                throw new ArgumentNullException("Assembly");
            }

            _compilation = GetCompilation();

            // If assembly is present, that means compilation was successful.
            IsCompilationSuccessful = true;

            try
            {
                memberInfo = _compilation.GetEntryPoint(asm);
                InitializeAttributes();
            }
            catch (Exception ex)
            {
                if (ex is ScriptCompilationException scriptEx)
                {
                    IsCompilationSuccessful = false;

                    if (scriptEx.CompilationOutput.Any())
                    {
                        CompilationOutput = CompilationOutput.Concat(scriptEx.CompilationOutput);
                    }

                    return;
                }

                throw;
            }
        }

        public async Task<object> Invoke(object[] parameters)
        {
            if (!IsCompilationSuccessful)
            {
                throw new ScriptCompilationException(CompilationOutput);
            }

            var methodInfo = memberInfo as MethodInfo;
            if (methodInfo == null)
            {
                throw new ScriptCompilationException("Failed to invoke non-method entity.");
            }

            int actualParameterCount = methodInfo.GetParameters().Length;
            parameters = parameters.Take(actualParameterCount).ToArray();

            object result = methodInfo.Invoke(null, parameters);

            if (result is Task)
            {
                result = await ((Task)result).ContinueWith(t => GetTaskResult(t));
            }

            return result;
        }

        public async Task<string> SaveAssemblyToDiskAsync(string assemblyPath)
        {
            var referenceResolver = new MemoryReferenceResolver(_frameworkLoads);
            ICompilationService compilationService = CompilationServiceFactory.CreateService(_entityMetaData, GetScriptOptions(referenceResolver));
            _compilation = await compilationService.GetCompilationAsync();
            _diagnostics = await _compilation.GetDiagnosticsAsync();
            References = referenceResolver.Used.ToImmutableArray();

            IsCompilationSuccessful = !_diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
            CompilationOutput = _diagnostics.Select(m => m.ToString());

            if (!IsCompilationSuccessful)
            {
                throw new ScriptCompilationException(CompilationOutput);
            }

            return await _compilation.SaveAssemblyAsync(assemblyPath);
        }

        public async Task<Tuple<string, string>> GetAssemblyBytesAsync()
        {
            var referenceResolver = new MemoryReferenceResolver(_frameworkLoads);
            ICompilationService compilationService = CompilationServiceFactory.CreateService(_entityMetaData, GetScriptOptions(referenceResolver));
            _compilation = await compilationService.GetCompilationAsync();
            _diagnostics = await _compilation.GetDiagnosticsAsync();
            References = referenceResolver.Used.ToImmutableArray();

            IsCompilationSuccessful = !_diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
            CompilationOutput = _diagnostics.Select(m => m.ToString());

            if (!IsCompilationSuccessful)
            {
                throw new ScriptCompilationException(CompilationOutput);
            }

            return await _compilation.GetAssemblyBytesAsync();
        }

        private void InitializeAttributes()
        {
            if (memberInfo == null)
            {
                return;
            }

            _entryPointDefinitionAttribute = memberInfo.GetCustomAttribute<Definition>();
            if (_entryPointDefinitionAttribute != null)
            {
                _entryPointDefinitionAttribute.SupportTopicList = memberInfo.GetCustomAttributes<SupportTopic>();
            }

            _resourceFilter = memberInfo.GetCustomAttribute<ResourceFilterBase>();
            _systemFilter = memberInfo.GetCustomAttribute<SystemFilter>();
        }

        private ScriptOptions GetScriptOptions(SourceReferenceResolver referenceResolver)
        {
            var scriptOptions = ScriptOptions.Default;

            if (!_frameworkReferences.IsDefaultOrEmpty)
            {
                scriptOptions = ScriptOptions.Default
                    .WithReferences(_frameworkReferences);
            }

            if (!_frameworkImports.IsDefaultOrEmpty)
            {
                scriptOptions = scriptOptions.WithImports(_frameworkImports);
            }

            scriptOptions = scriptOptions.WithSourceResolver(referenceResolver)
                .WithOptimizationLevel(OptimizationLevel.Release);

            return scriptOptions;
        }

        private void Validate()
        {
            if (this._entryPointDefinitionAttribute != null)
            {
                string id = this._entryPointDefinitionAttribute.Id;
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ScriptCompilationException("Id cannot be empty in Definition attribute");
                }

                if (string.IsNullOrWhiteSpace(this._entryPointDefinitionAttribute.Name))
                {
                    throw new ScriptCompilationException("Name cannot be empty in Definition attribute");
                }

                // Validate empty author
                if (string.IsNullOrWhiteSpace(this._entryPointDefinitionAttribute.Author))
                {
                    throw new ScriptCompilationException("Author not specified in Definition attribute");
                }

                List<char> invalidChars = Path.GetInvalidFileNameChars().ToList();
                invalidChars.ForEach(x =>
                {
                    if (id.Contains(x))
                    {
                        throw new ScriptCompilationException($"Id(in Definition attribute) cannot contain illegal character : {x}");
                    }

                    if (this._entryPointDefinitionAttribute.Author.Contains(x))
                    {
                        throw new ScriptCompilationException($"Author(in Definition attribute) cannot contain illegal character : {x}");
                    }
                });

                // Validate Support Topic Attributes
                if (this._entryPointDefinitionAttribute.SupportTopicList != null)
                {
                    this._entryPointDefinitionAttribute.SupportTopicList.ToList().ForEach(item =>
                    {
                        if (string.IsNullOrWhiteSpace(item.Id))
                        {
                            throw new ScriptCompilationException("Missing Id from Support Topic Attribute");
                        }

                        if (string.IsNullOrWhiteSpace(item.PesId))
                        {
                            throw new ScriptCompilationException("Missing PesId from Support Topic Attribute");
                        }
                    });

                    if (this._systemFilter == null && this._resourceFilter.InternalOnly && this._entryPointDefinitionAttribute.SupportTopicList.Any())
                    {
                        this.CompilationOutput = this.CompilationOutput.Concat(new string[] { "WARNING: Detector is marked internal and SupportTopic is specified. This means the detector will be enabled for Azure Support Center but not for case submission flow, until the isInternal flag is set to false." });
                    }
                }

                if (this._systemFilter != null && this._resourceFilter != null)
                {
                    throw new ScriptCompilationException("Detector is marked with both SystemFilter and ResourceFilter. System Invoker should not include any ResourceFilter attribute.");
                }
            }
        }

        private ICompilation GetCompilation()
        {
            switch (EntityMetadata.Type)
            {
                case EntityType.Analysis:
                    return new AnalysisCompilation();
                case EntityType.Detector:
                    return new DetectorCompilation();
                case EntityType.Signal:
                    return new SignalCompilation();
                case EntityType.Gist:
                    return new GistCompilation();
                default:
                    throw new NotSupportedException($"{EntityMetadata.Type} is not supported.");
            }
        }

        internal static object GetTaskResult(Task task)
        {
            if (task.IsFaulted)
            {
                throw task.Exception;
            }

            Type taskType = task.GetType();

            if (taskType.IsGenericType)
            {
                return taskType.GetProperty("Result").GetValue(task);
            }

            return null;
        }

        public void Dispose()
        {
            _compilation = null;
            memberInfo = null;
        }
    }
}
