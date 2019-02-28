// <copyright file="CompilationServiceTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.CompilationService;
using Diagnostics.Scripts.CompilationService.Gist;
using Diagnostics.Scripts.CompilationService.Interfaces;
using Diagnostics.Scripts.CompilationService.ReferenceResolver;
using Diagnostics.Scripts.Models;
using Diagnostics.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Xunit;

namespace Diagnostics.Tests.ScriptsTests
{
    /// <summary>
    /// Compilation service tests.
    /// </summary>
    public class CompilationServiceTests
    {
        #region Compilation Service Tests

        [Fact]
        public async void CompilationService_TestScriptCompilation()
        {
            var serviceInstance = CompilationServiceFactory.CreateService(ScriptTestDataHelper.GetRandomMetadata(), ScriptOptions.Default);
            ICompilation compilation = await serviceInstance.GetCompilationAsync();

            ImmutableArray<Diagnostic> diagnostics = await compilation.GetDiagnosticsAsync();
            Assert.Empty(diagnostics.Select(d => d.Severity == DiagnosticSeverity.Error));
        }

        [Fact]
        public async void CompilationService_TestGistCompilation()
        {
            var gist = ScriptTestDataHelper.GetGist();
            var metadata = new EntityMetadata(gist, EntityType.Gist);
            var scriptOptions = ScriptTestDataHelper.GetScriptOption(ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports());
            var serviceInstance = CompilationServiceFactory.CreateService(metadata, scriptOptions);
            var compilation = await serviceInstance.GetCompilationAsync();

            var diagnostics = await compilation.GetDiagnosticsAsync();
            Assert.Empty(diagnostics.Select(d => d.Severity == DiagnosticSeverity.Error));
        }

        [Fact]
        public async void CompilationService_TestGistEntryPointCheckFailure()
        {
            var gist = ScriptTestDataHelper.GetErrorGist();
            var metadata = new EntityMetadata(gist, EntityType.Gist);
            var serviceInstance = CompilationServiceFactory.CreateService(metadata, ScriptOptions.Default);
            var compilation = await serviceInstance.GetCompilationAsync();

            var diagnostics = await compilation.GetDiagnosticsAsync();
            Assert.Empty(diagnostics.Select(d => d.Severity == DiagnosticSeverity.Error));

            Exception ex = await Record.ExceptionAsync(async () =>
            {
                var entry = await compilation.GetEntryPoint();
            });

            Assert.NotNull(ex);
        }

        [Fact]
        public async void CompilationService_TestScriptCompilationFailure()
        {
            EntityMetadata metaData = ScriptTestDataHelper.GetRandomMetadata(EntityType.Detector);
            metaData.ScriptText = ScriptTestDataHelper.GetInvalidCsxScript(ScriptErrorType.CompilationError);

            var serviceInstance = CompilationServiceFactory.CreateService(metaData, ScriptOptions.Default);
            ICompilation compilation = await serviceInstance.GetCompilationAsync();

            ImmutableArray<Diagnostic> diagnostics = await compilation.GetDiagnosticsAsync();
            Assert.NotEmpty(diagnostics.Select(d => d.Severity == DiagnosticSeverity.Error));
        }

        [Fact]
        public async void CompilationService_TestVaildEntryPointResolution()
        {
            var serviceInstance = CompilationServiceFactory.CreateService(ScriptTestDataHelper.GetRandomMetadata(), ScriptOptions.Default);
            ICompilation compilation = await serviceInstance.GetCompilationAsync();

            Exception ex = Record.Exception(() =>
            {
                EntityMethodSignature methodSignature = compilation.GetEntryPointSignature();
            });

            Assert.Null(ex);
        }
        
        [Theory]
        [InlineData(ScriptErrorType.DuplicateEntryPoint)]
        [InlineData(ScriptErrorType.MissingEntryPoint)]
        public async void CompilationService_TestDuplicateEntryPoints(ScriptErrorType errorType)
        {
            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = ScriptTestDataHelper.GetInvalidCsxScript(errorType);

            var serviceInstance = CompilationServiceFactory.CreateService(metadata, ScriptOptions.Default);
            ICompilation compilation = await serviceInstance.GetCompilationAsync();

            ScriptCompilationException ex = Assert.Throws<ScriptCompilationException>(() =>
            {
                EntityMethodSignature methodSignature = compilation.GetEntryPointSignature();
            });

            Assert.NotEmpty(ex.CompilationOutput);
        }

        #endregion

        #region Compilation Service Factory Tests

        [Theory]
        [InlineData(EntityType.Signal, typeof(SignalCompilationService))]
        [InlineData(EntityType.Detector, typeof(DetectorCompilationService))]
        [InlineData(EntityType.Analysis, typeof(AnalysisCompilationService))]
        [InlineData(EntityType.Gist, typeof(GistCompilationService))]
        public void CompilationServiceFactory_GetServiceBasedOnType(EntityType type, object value)
        {
            EntityMetadata metaData = ScriptTestDataHelper.GetRandomMetadata(type);
            var compilationServiceInstance = CompilationServiceFactory.CreateService(metaData, ScriptOptions.Default);
            Assert.Equal(compilationServiceInstance.GetType(), value);
        }

        [Fact]
        public void CompilationServiceFactory_TestNullEntityMetadata()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var instance = CompilationServiceFactory.CreateService(null, ScriptOptions.Default);
            });
        }

        [Fact]
        public void CompilationServiceFactory_TestNullScriptOptions()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var instance = CompilationServiceFactory.CreateService(ScriptTestDataHelper.GetRandomMetadata(), null);
            });
        }

        [Fact]
        public void CompilationServiceFactory_TestForUnsupportedEntityType()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                var instance = CompilationServiceFactory.CreateService(new EntityMetadata(), ScriptOptions.Default);
            });
        }

        #endregion
    }
}
