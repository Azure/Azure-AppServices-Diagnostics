using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders.Interfaces
{
    public interface IKustoMap
    {
        string MapCluster(string cluster);
        string MapDatabase(string database);
        bool TryGetCluster(KustoDatabaseEntry database, out string targetCluster);
        bool TryGetCluster(KustoDatabaseEntry database, string environment, out string targetCluster);
        bool TryGetDatabase(KustoClusterEntry cluster, out string targetDatabase);
        bool TryGetDatabase(KustoClusterEntry cluster, string environment, out string targetDatabase);
    }
}
