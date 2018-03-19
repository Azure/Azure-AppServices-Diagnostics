using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Diagnostics.ModelsAndUtils
{
    public class ScriptHelper
    {
        public static ImmutableArray<string> GetFrameworkReferences() => ImmutableArray.Create(
                "System",
                "System.Data",
                "System.IO",
                "System.Linq",
                "Diagnostics.DataProviders",
                "Diagnostics.ModelsAndUtils"
            );

        public static ImmutableArray<string> GetFrameworkImports() => ImmutableArray.Create(
                "System",
                "System.Collections",
                "System.Collections.Concurrent",
                "System.Collections.Generic",
                "System.Data",
                "System.IO",
                "System.Linq",
                "System.Threading.Tasks",
                "Diagnostics.DataProviders",
                "Diagnostics.ModelsAndUtils"
            );
    }
}
