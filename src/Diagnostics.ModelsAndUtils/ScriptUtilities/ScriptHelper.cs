using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Diagnostics.ModelsAndUtils.ScriptUtilities
{
    public class ScriptHelper
    {
        public static ImmutableArray<string> GetFrameworkReferences() => ImmutableArray.Create(
                "System",
                "System.Collections",
                "System.Data",
                "Diagnostics.DataProviders",
                "Diagnostics.ModelsAndUtils",
                "Newtonsoft.Json"
            );

        public static ImmutableArray<string> GetFrameworkImports() => ImmutableArray.Create(
                "System",
                "System.Collections",
                "System.Collections.Generic",
                "System.Data",
                "System.Threading.Tasks",
                "Diagnostics.DataProviders",
                "Diagnostics.ModelsAndUtils",
                "Diagnostics.ModelsAndUtils.Models",
                "Diagnostics.ModelsAndUtils.ScriptUtilities",
                "Diagnostics.ModelsAndUtils.Models.ResponseExtensions",
                "Newtonsoft.Json"
            );
    }
}
