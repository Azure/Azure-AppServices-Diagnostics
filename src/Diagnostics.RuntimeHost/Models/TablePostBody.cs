using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Models
{
    public sealed class TablePostBody
    {
        public IEnumerable<string> Columns { get; set; }
        public IEnumerable<string[]> Rows { get; set; }
    }
}
