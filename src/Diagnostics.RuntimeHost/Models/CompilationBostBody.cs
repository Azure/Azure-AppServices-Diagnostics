using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Models
{
    public class CompilationBostBody<T>
    {
        private string _sanitizedScript;

        public string Script
        {
            get
            {
                return _sanitizedScript;
            }
            set
            {
                _sanitizedScript = FileHelper.SanitizeScriptFile(value);
            }
        }

        public T Resource;
    }
}
