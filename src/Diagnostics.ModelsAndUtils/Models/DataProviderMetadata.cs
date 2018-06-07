using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class DataProviderMetadata
    {
        public string ProviderName;
        public List<KeyValuePair<string, object>> PropertyBag { get; }

        public DataProviderMetadata()
        {
            PropertyBag = new List<KeyValuePair<string, object>>();
        }
    }
}
