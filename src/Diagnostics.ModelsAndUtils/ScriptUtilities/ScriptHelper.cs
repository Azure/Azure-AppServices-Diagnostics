using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Diagnostics.ModelsAndUtils
{
    public class ScriptHelper
    {
        public static ImmutableArray<string> GetFrameworkReferences() => ImmutableArray.Create(
                "System.Data",
                "Diagnostics.DataProviders",
                "Diagnostics.ModelsAndUtils"
            );

        public static ImmutableArray<string> GetFrameworkImports() => ImmutableArray.Create(
                "System.Data",
                "System.Threading.Tasks",
                "Diagnostics.DataProviders",
                "Diagnostics.ModelsAndUtils"
            );
    }
}
