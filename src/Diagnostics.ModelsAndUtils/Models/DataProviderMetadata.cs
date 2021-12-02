using System.Collections.Generic;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class DataProviderMetadata
    {
        public string ProviderName { get; set; }
        public List<KeyValuePair<string, object>> PropertyBag { get; }

        public DataProviderMetadata()
        {
            PropertyBag = new List<KeyValuePair<string, object>>();
        }
    }
}
