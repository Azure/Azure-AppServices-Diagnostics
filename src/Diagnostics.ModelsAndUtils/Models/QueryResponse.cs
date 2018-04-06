using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class QueryResponse<T>
    {
        public CompilerResponse CompilationOutput;

        public T InvocationOutput;
    }
}
