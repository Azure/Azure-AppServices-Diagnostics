using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Diagnostics.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace Diagnostics.Tests.ScriptsTests
{
    public class EntityInvokerTests
    {
        [Fact]
        public async void EntityInvoker_TestInvokeMethod()
        {
            using (EntityInvoker invoker = new EntityInvoker(ScriptTestDataHelper.GetRandomMetadata(), ImmutableArray.Create<string>()))
            {
                await invoker.InitializeEntryPointAsync();
                int result = (int)await invoker.Invoke(new object[] { 3 });
                Assert.Equal(9, result);
            }
        }

        [Theory]
        [InlineData(ResourceType.App, typeof(AppFilter))]
        [InlineData(ResourceType.HostingEnvironment, typeof(HostingEnvironmentFilter))]
        public async void EntityInvoker_TestResourceAttributeResolution(ResourceType resType, Type filterType)
        {
            Definition definitonAttribute = new Definition()
            {
                Id = "TestId"
            };
            
            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = await ScriptTestDataHelper.GetDetectorScript(definitonAttribute, resType);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                Assert.Equal<Definition>(definitonAttribute, invoker.EntryPointDefinitionAttribute);
                Assert.Equal(filterType, invoker.ResourceFilter.GetType());
            }
        }

        [Fact]
        public async void EntityInvoker_TestSupportTopicAttributeResolution()
        {
            Definition definitonAttribute = new Definition()
            {
                Id = "TestId",
                Name = "Test",
                Author = "User"
            };

            SupportTopic topic1 = new SupportTopic() { Id = "1234", PesId = "14878" };
            SupportTopic topic2 = new SupportTopic() { Id = "5678", PesId = "14878" };

            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = await ScriptTestDataHelper.GetDetectorScriptWithMultipleSupportTopics(definitonAttribute, false, topic1, topic2);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();

                Assert.True(invoker.IsCompilationSuccessful);
                Assert.Contains<SupportTopic>(topic1, invoker.EntryPointDefinitionAttribute.SupportTopicList);
                Assert.Contains<SupportTopic>(topic2, invoker.EntryPointDefinitionAttribute.SupportTopicList);
            }
        }

        [Theory]
        [InlineData("", "14878", false)]
        [InlineData("1234", "", false)]
        [InlineData("1234", "14878", true)]
        public async void EntityInvoker_InvalidSupportTopicId(string supportTopicId, string pesId, bool isInternal)
        {
            Definition definitonAttribute = new Definition()
            {
                Id = "TestId",
                Name = "Test",
                Author = "User"
            };

            SupportTopic topic1 = new SupportTopic() { Id = supportTopicId, PesId = pesId };
            SupportTopic topic2 = new SupportTopic() { Id = "5678", PesId = "14878" };

            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = await ScriptTestDataHelper.GetDetectorScriptWithMultipleSupportTopics(definitonAttribute, isInternal, topic1, topic2);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();

                Assert.False(invoker.IsCompilationSuccessful);
                Assert.NotEmpty(invoker.CompilationOutput);
            }
        }

        [Fact]
        public async void EntityInvoker_TestGetAssemblyBytes()
        {
            Definition definitonAttribute = new Definition()
            {
                Id = "TestId"
            };

            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = await ScriptTestDataHelper.GetDetectorScript(definitonAttribute);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                Tuple<string, string> asmPair = await invoker.GetAssemblyBytesAsync();

                string assemblyBytes = asmPair.Item1;
                string pdbBytes = asmPair.Item2;

                Assert.False(string.IsNullOrWhiteSpace(assemblyBytes));
                Assert.False(string.IsNullOrWhiteSpace(pdbBytes));
            }
        }

        [Fact]
        public async void EntityInvoker_TestSaveAssemblyToDisk()
        {
            Definition definitonAttribute = new Definition()
            {
                Id = "TestId"
            };

            string assemblyPath = $@"{Directory.GetCurrentDirectory()}\{Guid.NewGuid().ToString()}";
            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = await ScriptTestDataHelper.GetDetectorScript(definitonAttribute);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                await invoker.SaveAssemblyToDiskAsync(assemblyPath);

                Assert.True(File.Exists($"{assemblyPath}.dll"));
                Assert.True(File.Exists($"{assemblyPath}.pdb"));
            }
        }

        [Fact]
        public async void EntityInvoker_TestInitializationUsingAssembly()
        {
            // First Create and Save a assembly for test purposes.
            Definition definitonAttribute = new Definition()
            {
                Id = "TestId"
            };

            string assemblyPath = $@"{Directory.GetCurrentDirectory()}/{Guid.NewGuid().ToString()}";
            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = await ScriptTestDataHelper.GetDetectorScript(definitonAttribute);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                await invoker.SaveAssemblyToDiskAsync(assemblyPath);

                Assert.True(File.Exists($"{assemblyPath}.dll"));
                Assert.True(File.Exists($"{assemblyPath}.pdb"));
            }

            // Now test initializing Entry Point of Invoker using assembly
            Assembly asm = Assembly.LoadFrom($"{assemblyPath}.dll");

            using (EntityInvoker invoker = new EntityInvoker(metadata))
            {
                Exception ex = Record.Exception(() =>
                {
                    invoker.InitializeEntryPoint(asm);
                });

                Assert.Null(ex);
                Assert.True(invoker.IsCompilationSuccessful);
                Assert.Equal(definitonAttribute.Id, invoker.EntryPointDefinitionAttribute.Id);
            }
        }

        [Theory]
        [InlineData(ScriptErrorType.CompilationError)]
        [InlineData(ScriptErrorType.DuplicateEntryPoint)]
        [InlineData(ScriptErrorType.MissingEntryPoint)]
        public async void EntityInvoker_TestInvokeWithCompilationError(ScriptErrorType errorType)
        {
            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = ScriptTestDataHelper.GetInvalidCsxScript(errorType);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ImmutableArray.Create<string>()))
            {
                ScriptCompilationException ex = await Assert.ThrowsAsync<ScriptCompilationException>(async () =>
                {
                    await invoker.InitializeEntryPointAsync();
                    int result = (int)await invoker.Invoke(new object[] { 3 });
                    Assert.Equal(9, result);
                });

                Assert.NotEmpty(ex.CompilationOutput);
            }
        }

        [Fact]
        public async void EntityInvoker_TestSaveAssemblyToInvalidPath()
        {
            Definition definitonAttribute = new Definition()
            {
                Id = "TestId"
            };

            string assemblyPath = string.Empty;
            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = await ScriptTestDataHelper.GetDetectorScript(definitonAttribute);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();

                await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                {
                    await invoker.SaveAssemblyToDiskAsync(assemblyPath);
                });
            }
        }

        [Theory]
        [InlineData("")]
        public async void EntityInvoker_InvalidDetectorId(string idValue)
        {
            Definition def = new Definition() { Id = idValue };
            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = await ScriptTestDataHelper.GetDetectorScript(def, ResourceType.App);
            
            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();

                Assert.False(invoker.IsCompilationSuccessful);
                Assert.NotEmpty(invoker.CompilationOutput);
            }
        }
    }
}
