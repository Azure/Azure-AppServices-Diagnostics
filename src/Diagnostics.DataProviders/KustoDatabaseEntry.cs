using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders
{
    public sealed class KustoDatabaseEntry : IEquatable<KustoDatabaseEntry>
    {
        public string Value { get; private set; }
        
        public KustoDatabaseEntry(string databaseName)
        {
            Value = databaseName;
        }

        public bool Equals(KustoDatabaseEntry other)
        {
            return this.Value.Equals(other.Value, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
