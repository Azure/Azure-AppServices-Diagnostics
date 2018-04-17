using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Models
{
    public class CompilationBostBody<T>
    {
        public string Script;
        public T Resource;
    }
}
