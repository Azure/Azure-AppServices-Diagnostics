using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils
{
    public class QueryResponse<T>
    {
        public CompilerResponse CompilationOutput;

        public T InvocationOutput;
    }
}
