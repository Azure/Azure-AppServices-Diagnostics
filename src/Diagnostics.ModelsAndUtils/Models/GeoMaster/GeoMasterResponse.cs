using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class GeoMasterResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Location { get; set; }
        public IDictionary<string,string> Tags { get; set; }
        public IDictionary<string, dynamic> Properties { get; set; }
    }

    public class GeoMasterResponseValue
    {
        public GeoMasterResponse[] Value;
    }

}
