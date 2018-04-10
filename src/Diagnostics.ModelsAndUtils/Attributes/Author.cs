using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    public class Author: Attribute
    {
        public string[] Authors { get; set; }
    }
}
