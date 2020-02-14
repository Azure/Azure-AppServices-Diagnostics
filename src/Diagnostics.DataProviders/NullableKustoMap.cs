using Diagnostics.DataProviders.Interfaces;

namespace Diagnostics.DataProviders
{
    internal sealed class NullableKustoMap : IKustoMap
    {
        public string MapCluster(string cluster)
        {
            return null;
        }

        public string MapDatabase(string database)
        {
            return null;
        }

        public bool TryGetCluster(KustoDatabaseEntry database, out string targetCluster)
        {
            targetCluster = null;
            return false;
        }

        public bool TryGetCluster(KustoDatabaseEntry database, string environment, out string targetCluster)
        {
            return TryGetCluster(database, out targetCluster);
        }

        public bool TryGetDatabase(KustoClusterEntry cluster, out string targetDatabase)
        {
            targetDatabase = null;
            return false;
        }

        public bool TryGetDatabase(KustoClusterEntry cluster, string environment, out string targetDatabase)
        {
            return TryGetDatabase(cluster, out targetDatabase);
        }
    }
}
