// <copyright file="GistCompilation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Diagnostics.Scripts.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Diagnostics.Scripts.CompilationService.Gist
{
    /// <summary>
    /// Gist compilation.
    /// </summary>
    public class GistCompilation : CompilationBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GistCompilation"/> class.
        /// </summary>
        public GistCompilation()
            : base(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GistCompilation"/> class.
        /// </summary>
        /// <param name="compilation">The compilation.</param>
        public GistCompilation(Compilation compilation)
            : base(compilation)
        {
        }

        /// <summary>
        /// Get entry point for gist.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>Member info.</returns>
        public override MemberInfo GetEntryPoint(Assembly assembly)
        {
            if (_compilation == null)
            {
                // Try to get the types with attributes.
                var types = assembly.GetTypes().Where(t => t.CustomAttributes.Any() && Attribute.GetCustomAttribute(t, typeof(CompilerGeneratedAttribute)) == null);
                if (!types.Any() || types.Count() > 1)
                {
                    throw new ScriptCompilationException("One class is allowed in one gist file.");
                }

                return types.First().GetTypeInfo();
            }

            var symbols = _compilation.ScriptClass.GetMembers().OfType<INamedTypeSymbol>();

            if (!symbols.Any() || symbols.Count() > 1)
            {
                throw new ScriptCompilationException("One class is allowed in one gist file.");
            }

            var type = assembly.GetTypes().FirstOrDefault(t => t.Name.Equals(symbols.First().Name));
            return type.GetTypeInfo();
        }

        /// <summary>
        /// Get code analyzers.
        /// </summary>
        /// <returns>Diagnostic analyzers.</returns>
        protected override ImmutableArray<DiagnosticAnalyzer> GetCodeAnalyzers()
        {
            return ImmutableArray.Create<DiagnosticAnalyzer>();
        }
    }
}
