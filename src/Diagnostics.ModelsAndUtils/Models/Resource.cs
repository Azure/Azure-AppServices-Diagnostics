using Diagnostics.ModelsAndUtils.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    //public sealed class SiteResource : Resource
    //{
    //    public string SiteName;

    //    public IEnumerable<string> HostNames;

    //    public string Stamp;

    //    public string SourceMoniker {
    //        get
    //        {
    //            return string.IsNullOrWhiteSpace(Stamp) ? Stamp.ToUpper().Replace("-", string.Empty) : string.Empty;
    //        }
    //    }
    //}
    
    public class Hostname
    {
        public string Name { get; set; }

        public int Type { get; set; }
    }
}
