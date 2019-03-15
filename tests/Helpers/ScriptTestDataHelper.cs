using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.Scripts.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.Tests.Helpers
{
    public static class ScriptTestDataHelper
    {

        public static EntityMetadata GetRandomMetadata(EntityType type = EntityType.Signal)
        {
            return new EntityMetadata()
            {
                ScriptText = GetNumSqaureScript(),
                Type = type
            };
        }

        public static string GetNumSqaureScript()
        {
            return @"
                public static int Run(int x) {
                    x = x * x;
                    return x;
                }";
        }

        public static string GetInvalidCsxScript(ScriptErrorType errorType)
        {
            switch (errorType)
            {
                case ScriptErrorType.MissingEntryPoint:
                    return @"
                        public static string SomeMethod() => ""test string"";
                    ";
                case ScriptErrorType.DuplicateEntryPoint:
                    return @"
                        public static int Run(int x) {
                            return x * x;
                        }
                        public static int Run(int x, int y) {
                            return x + y;
                        }
                    ";
                case ScriptErrorType.CompilationError:
                default:
                    return @"
                        public static int Run(int x) {
                        return x * x
                    }";
            }
        }

        public static async Task<string> GetDetectorScript(Definition def, ResourceType resourceType = ResourceType.App, string kustoTableName = "MockTable", string queryPart = "take 1")
        {
            string kustoTemplate = string.Empty;
            if(resourceType == ResourceType.HostingEnvironment)
            {
                kustoTemplate = await File.ReadAllTextAsync(@"templates/Detector_HostingEnvironment.csx");
            }
            else
            {
                kustoTemplate = await File.ReadAllTextAsync(@"templates/Detector_WebApp.csx");
            }

            return kustoTemplate.Replace("<YOUR_DETECTOR_ID>", def.Id)
                .Replace("<YOUR_TABLE_NAME>", kustoTableName)
                .Replace("<YOUR_QUERY>", queryPart);
        }

        public static async Task<string> GetGistScript(Definition def, ResourceType resourceType = ResourceType.App)
        {
            string template;
            if (resourceType == ResourceType.HostingEnvironment)
            {
                template = await File.ReadAllTextAsync(@"templates/Gist_HostingEnvironment.csx");
            }
            else
            {
                template = await File.ReadAllTextAsync(@"templates/Gist_WebApp.csx");
            }

            template = template.Replace("<YOUR_CLASS_NAME>", "GistClass");

            return template.Replace("<YOUR_GIST_ID>", def.Id);
        }

        public static async Task<string> GetDetectorScriptWithMultipleSupportTopics(Definition def, bool isInternal, SupportTopic topic1, SupportTopic topic2)
        {
            string template = await File.ReadAllTextAsync(@"TestData/TestDetectorWithSupportTopic.csx");

            return template.Replace("<YOUR_DETECTOR_ID>", def.Id)
                .Replace("<YOUR_DETECTOR_NAME>", def.Name)
                .Replace("<YOUR_ALIAS>", def.Author)
                .Replace("<SUPPORT_TOPIC_ID_1>", topic1.Id.ToString())
                .Replace("<SUPPORT_TOPIC_ID_2>", topic2.Id.ToString())
                .Replace("<PES_ID_1>", topic1.PesId.ToString())
                .Replace("<PES_ID_2>", topic2.PesId.ToString())
                .Replace("\"<INTERNAL_FLAG>\"", isInternal ? "true" : "false");
        }

        public static async Task<string> GetSystemInvokerScript(Definition def)
        {
            string template = await File.ReadAllTextAsync(@"templates/SystemInvokerTest.csx");
            string query = "cluster('MockCluster).database('Mockdb').DiagnosticRole | top 1 by PreciseTimeStamp desc";
            return template.Replace("<YOUR_DETECTOR_ID>", def.Id)
                .Replace("<YOUR_DETECTOR_NAME>", def.Name)
                .Replace("<YOUR_ALIAS>", def.Author)
                .Replace("<YOUR_QUERY>", query);
        }

        public static string GetGist()
        {
            return @"public static class A{}";
        }

        public static string GetErrorGist()
        {
            return @"public static class A{};
                    public static class B{}";
        }

        public static string GetSentinel()
        {
            return @"
                #load ""xxx""
                #load ""yyy""

                using System;

                [AppFilter(AppType = AppType.All, PlatformType = PlatformType.Windows, StackType = StackType.All)]
                [Definition(Id = ""id"", Name = ""name"", Author = ""authors"", Description = ""description"", Category = """")]
                public static string Run()
                {
                    return ""Test"";
                }";
        }

        public static ScriptOptions GetScriptOption(ImmutableArray<string> frameworkReferences, ImmutableArray<string> frameworkImports, SourceReferenceResolver resolver = null)
        {
            var scriptOptions = ScriptOptions.Default;

            if (!frameworkReferences.IsDefaultOrEmpty)
            {
                scriptOptions = scriptOptions.WithReferences(frameworkReferences);
            }

            if (!frameworkImports.IsDefaultOrEmpty)
            {
                scriptOptions = scriptOptions.WithImports(frameworkImports);
            }

            if (resolver != null)
            {
                scriptOptions = scriptOptions.WithSourceResolver(resolver);
            }

            return scriptOptions;
        }
    }
}
