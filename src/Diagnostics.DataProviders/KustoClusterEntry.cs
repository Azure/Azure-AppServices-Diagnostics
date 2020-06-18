using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders
{
    public sealed class KustoClusterEntry : IEquatable<KustoClusterEntry>
    {
        public string Value { get; private set; }
        
        public KustoClusterEntry(string clusterName)
        {
            Value = clusterName;
        }

        public bool Equals(KustoClusterEntry other)
        {
            return this.Value.Equals(other.Value, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
