using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class SearchResult
    {
        public string Detector { get; set; }

        public float Score { get; set; }
    }

    public class SearchResults
    {
        public string Query { get; set; }
        public SearchResult[] Results { get; set; }
    }
}
