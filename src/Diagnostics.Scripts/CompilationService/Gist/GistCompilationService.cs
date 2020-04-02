// <copyright file="GistCompilationService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System.Threading.Tasks;
using Diagnostics.Scripts.CompilationService.Interfaces;
using Diagnostics.Scripts.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace Diagnostics.Scripts.CompilationService.Gist
{
    /// <summary>
    /// Gist compilation service.
    /// </summary>
    public class GistCompilationService : CompilationServiceBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GistCompilationService"/> class.
        /// </summary>
        /// <param name="entityMetadata">Entity metadata.</param>
        /// <param name="scriptOptions">Script options.</param>
        public GistCompilationService(EntityMetadata entityMetadata, ScriptOptions scriptOptions)
            : base(entityMetadata, scriptOptions)
        {
        }

        /// <summary>
        /// Create compilation object.
        /// </summary>
        /// <param name="scriptCompilation">Compilation result.</param>
        /// <returns>Task for creating compilation object.</returns>
        protected override Task<ICompilation> CreateCompilationObject(Compilation scriptCompilation)
        {
            return Task.FromResult<ICompilation>(new GistCompilation(scriptCompilation));
        }
    }
}
