using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Diagnostics.Tests.Helpers;
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

        [Theory]
        [InlineData(ResourceType.App, typeof(AppFilter))]
        [InlineData(ResourceType.HostingEnvironment, typeof(HostingEnvironmentFilter))]
        public async void EntityInvoker_TestGistResourceAttributeResolution(ResourceType resType, Type filterType)
        {
            Definition definitonAttribute = new Definition()
            {
                Id = "TestId",
                Author = "Test Author",
                Name = "Test Gist"
            };

            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata(EntityType.Gist);
            metadata.ScriptText = await ScriptTestDataHelper.GetGistScript(definitonAttribute, resType);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                Assert.Equal(definitonAttribute, invoker.EntryPointDefinitionAttribute);
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
            SupportTopic topic3 = new SupportTopic() { Id = "5678", PesId = "14878", SapSupportTopicId = "22f427b1-2ef1-3b71-0082-e2a6f3805280", SapProductId = "272fd66a-e8b1-260f-0066-01caae8895cf" };


            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = await ScriptTestDataHelper.GetDetectorScriptWithMultipleSupportTopics(definitonAttribute, false, topic1, topic2, topic3);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();

                Assert.True(invoker.IsCompilationSuccessful);
                Assert.Contains<SupportTopic>(topic1, invoker.EntryPointDefinitionAttribute.SupportTopicList);
                Assert.Contains<SupportTopic>(topic2, invoker.EntryPointDefinitionAttribute.SupportTopicList);
                Assert.Contains<SupportTopic>(topic3, invoker.EntryPointDefinitionAttribute.SupportTopicList);
            }
        }

        [Theory]
        [InlineData("", "14878", false)]
        [InlineData("1234", "", false)]
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
            SupportTopic topic3 = new SupportTopic() { Id = "1234", PesId = "14878" };

            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = await ScriptTestDataHelper.GetDetectorScriptWithMultipleSupportTopics(definitonAttribute, isInternal, topic1, topic2, topic3);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();

                Assert.False(invoker.IsCompilationSuccessful);
                Assert.NotEmpty(invoker.CompilationOutput);
            }
        }


        [Theory]
        [InlineData("d4e5a23b-45f1-e516-36c7-80ef5201d700", "", false)]
        [InlineData("", "272fd66a-e8b1-260f-0066-01caae8895cf", false)]
        public async void EntityInvoker_InvalidSapSupportTopicId(string sapSupportTopicId, string sapProductId, bool isInternal)
        {
            Definition definitonAttribute = new Definition()
            {
                Id = "TestId",
                Name = "Test",
                Author = "User"
            };

            SupportTopic topic1 = new SupportTopic() { SapSupportTopicId = sapSupportTopicId, SapProductId = sapProductId };
            SupportTopic topic2 = new SupportTopic() { SapSupportTopicId = "d40f17bb-8b19-117c-f69a-d1be4187f657", SapProductId = "272fd66a-e8b1-260f-0066-01caae8895cf" };
            SupportTopic topic3 = new SupportTopic() { SapSupportTopicId = "22f427b1-2ef1-3b71-0082-e2a6f3805280", SapProductId = "272fd66a-e8b1-260f-0066-01caae8895cf" };

            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = await ScriptTestDataHelper.GetDetectorScriptWithMultipleSupportTopics(definitonAttribute, isInternal, topic1, topic2, topic3);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();

                Assert.False(invoker.IsCompilationSuccessful);
                Assert.NotEmpty(invoker.CompilationOutput);
            }
        }

        [Fact]
        public async void EntityInvoker_TestSystemFilterAttributeResolution()
        {
            Definition definitonAttribute = new Definition()
            {
                Id = "TestId",
                Name = "Test",
                Author = "User"
            };

            SystemFilter filter = new SystemFilter();
            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata();
            metadata.ScriptText = await ScriptTestDataHelper.GetSystemInvokerScript(definitonAttribute);

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();

                Assert.True(invoker.IsCompilationSuccessful);
                Assert.NotNull(invoker.SystemFilter);
                Assert.Equal(filter, invoker.SystemFilter);
            }
        }

        [Fact]
        public async void EntityInvoker_TestDetectorWithGists()
        {
            var gist = ScriptTestDataHelper.GetGist();
            var references = new Dictionary<string, string>
            {
                { "xxx", gist },
                { "yyy", "" },
                { "zzz", "" }
            };

            var metadata = new EntityMetadata(ScriptTestDataHelper.GetSentinel(), EntityType.Detector);
            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports(), references.ToImmutableDictionary()))
            {
                await invoker.InitializeEntryPointAsync();
                var result = (string)await invoker.Invoke(new object[] { });
                Assert.Equal(2, invoker.References.Count());
                Assert.False(string.IsNullOrWhiteSpace(result));
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

            string assemblyPath = $@"{AppContext.BaseDirectory}\{Guid.NewGuid().ToString()}";
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
        public async void EntityInvoker_TestGistInitializationUsingAssembly()
        {
            // First Create and Save a assembly for test purposes.
            Definition definitonAttribute = new Definition()
            {
                Id = "TestId",
                Author = "Test Author",
                Name = "Test Name"
            };

            string assemblyPath = $@"{AppContext.BaseDirectory}/{Guid.NewGuid().ToString()}";
            EntityMetadata metadata = ScriptTestDataHelper.GetRandomMetadata(EntityType.Gist);
            metadata.ScriptText = await ScriptTestDataHelper.GetGistScript(definitonAttribute);

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

        [Fact]
        public async void EntityInvoker_TestInitializationUsingAssembly()
        {
            // First Create and Save a assembly for test purposes.
            Definition definitonAttribute = new Definition()
            {
                Id = "TestId"
            };

            string assemblyPath = $@"{AppContext.BaseDirectory}/{Guid.NewGuid().ToString()}";
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

        [Fact]
        public async void EntityInvoker_TestValidShareableGists()
        {
            Definition definitonAttribute = new Definition()
            {
                Id = "testGistId",
                Author = "testAuthor",
                Name = "Test Gist"
            };

            string assemblyPath = string.Empty;
            string gistScript = await ScriptTestDataHelper.GetGistScript(definitonAttribute, ResourceType.ArmResource);

            // 1. Testing Gist Compilation that are shared globally
            EntityMetadata metadata  = new EntityMetadata(gistScript, EntityType.Gist);
            metadata.ScriptText = gistScript.Replace("Microsoft.EventHub", "*").Replace("namespaces", "*");
            

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                Assert.True(invoker.IsCompilationSuccessful);
            }

            // 2. Testing Gist Compilation that are shared within one RP only
            metadata.ScriptText = gistScript.Replace("namespaces", "*");

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                Assert.True(invoker.IsCompilationSuccessful);
            }
        }

        [Fact]
        public async void EntityInvoker_TestInvalidShareableGist()
        {
            Definition definitonAttribute = new Definition()
            {
                Id = "testGistId",
                Author = "testAuthor",
                Name = "Test Gist"
            };

            string assemblyPath = string.Empty;
            string gistScript = await ScriptTestDataHelper.GetGistScript(definitonAttribute, ResourceType.ArmResource);

            EntityMetadata metadata = new EntityMetadata(gistScript, EntityType.Gist);
            metadata.ScriptText = gistScript.Replace("Microsoft.EventHub", "*");

            using (EntityInvoker invoker = new EntityInvoker(metadata, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                await invoker.InitializeEntryPointAsync();
                Assert.False(invoker.IsCompilationSuccessful);
            }
        }
    }
}
