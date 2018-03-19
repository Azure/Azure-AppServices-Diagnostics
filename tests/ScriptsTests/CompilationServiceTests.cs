using Diagnostics.Scripts;
using Diagnostics.Scripts.CompilationService;
using Diagnostics.Scripts.CompilationService.Interfaces;
using Diagnostics.Scripts.Models;
using Diagnostics.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Xunit;

namespace Diagnostics.Tests.ScriptsTests
{
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
