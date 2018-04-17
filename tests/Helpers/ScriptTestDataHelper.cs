using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.Scripts.Models;
using System;
using System.Collections.Generic;
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

        public static async Task<string> GetDetectorScript(string id, ResourceType resourceType = ResourceType.App, string kustoTableName = "MockTable", string queryPart = "take 1")
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

            return kustoTemplate.Replace("<YOUR_DETECTOR_ID>", id)
                .Replace("<YOUR_TABLE_NAME>", kustoTableName)
                .Replace("<YOUR_QUERY>", queryPart);
        }
    }
}
