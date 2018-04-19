using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    public class Authors: Attribute
    {
        [DataMember]
        public string[] Values { get; set; }

        public Authors(params string[] values)
        {
            this.Values = values;
        }
    }
}
