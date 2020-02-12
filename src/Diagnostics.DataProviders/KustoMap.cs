using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Diagnostics.DataProviders.Interfaces;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// KustoMap data structure to provide correct mapping depending on cloud environment
    /// </summary>
    public sealed class KustoMap : List<Dictionary<string, string>>, IKustoMap, IDisposable
    {
        private DataTable _clusterMapping;
        private DataTable _databaseMapping;
        private string[] environments = new string[]
        {
            DataProviderConstants.AzureCloud,
            DataProviderConstants.AzureCloudAlternativeName,
            DataProviderConstants.AzureChinaCloud,
            DataProviderConstants.AzureChinaCloudCodename,
            DataProviderConstants.AzureUSGovernment,
            DataProviderConstants.AzureUSGovernmentCodename,
            DataProviderConstants.AzureUSNat,
            DataProviderConstants.AzureUSSec
        };

        private string _cloudEnvironment;

        public KustoMap(string cloudEnvironment) : this(cloudEnvironment, new List<Dictionary<string, string>>())
        {
        }

        public KustoMap(string cloudEnvironment, List<Dictionary<string, string>> existingMap)
        {
            if (existingMap == null)
            {
                throw new ArgumentNullException();
            }

            if (string.IsNullOrWhiteSpace(cloudEnvironment))
            {
                throw new ArgumentNullException();
            }

            _cloudEnvironment = cloudEnvironment;
            _clusterMapping = new DataTable();
            _databaseMapping = new DataTable();
            _clusterMapping.CaseSensitive = false;
            _databaseMapping.CaseSensitive = false;
            
            foreach (var env in environments)
            {
                _clusterMapping.Columns.Add(new DataColumn { ColumnName = env, DataType = typeof(string), AllowDBNull = true });
                _databaseMapping.Columns.Add(new DataColumn { ColumnName = env, DataType = typeof(string), AllowDBNull = true });
            }

            foreach (IEnumerable<KeyValuePair<string,string>> bodyOfKeyValuePairs in existingMap)
            {
                var clusterRow = _clusterMapping.NewRow();
                var databaseRow = _databaseMapping.NewRow();

                foreach (KeyValuePair<string, string> pair in bodyOfKeyValuePairs)
                {
                    foreach (string env in environments)
                    {
                        if (pair.Key.ToLower().Contains(env.ToLower()))
                        {
                            if (pair.Key.ToLower().Contains("clustername"))
                            {
                                if (!string.IsNullOrWhiteSpace(pair.Value))
                                {
                                    clusterRow[env] = pair.Value;
                                }
                            }
                            else if (pair.Key.ToLower().Contains("databasename"))
                            {
                                if (!string.IsNullOrWhiteSpace(pair.Value))
                                {
                                    databaseRow[env] = pair.Value;
                                }
                            }
                        }
                    }
                }
                 
                _clusterMapping.Rows.Add(clusterRow);
                _databaseMapping.Rows.Add(databaseRow);
            }

            //todo add logging of entries that didn't get added to the map
        }

        public string MapCluster(string srcCluster)
        {
            DataRow sourceRow = null;

            if (_clusterMapping.Columns.Contains(_cloudEnvironment))
            {
                foreach (DataColumn column in _clusterMapping.Columns)
                {
                    if (sourceRow != null)
                    {
                        return sourceRow[_cloudEnvironment]?.ToString();
                    }

                    foreach (DataRow row in _clusterMapping.Rows)
                    {
                        if (row[column].ToString().Equals(srcCluster, StringComparison.CurrentCultureIgnoreCase))
                        {
                            sourceRow = row;
                            break;
                        }
                    }
                }
            }

            return null;
        }

        public string MapDatabase(string srcDatabase)
        {
            DataRow sourceRow = null;

            if (_databaseMapping.Columns.Contains(_cloudEnvironment))
            {
                foreach (DataColumn column in _databaseMapping.Columns)
                {
                    if (sourceRow != null)
                    {
                        return sourceRow[_cloudEnvironment]?.ToString();
                    }

                    foreach (DataRow row in _databaseMapping.Rows)
                    {
                        if (row[column].ToString().Equals(srcDatabase, StringComparison.CurrentCultureIgnoreCase))
                        {
                            sourceRow = row;
                            break;
                        }
                    }
                }
            }

            return null;
        }

        public void Dispose()
        {
            _clusterMapping.Dispose();
            _databaseMapping.Dispose();
        }

        public bool TryGetCluster(KustoDatabaseEntry database, out string targetCluster)
        {
            return TryGetCluster(database, _cloudEnvironment, out targetCluster);
        }

        public bool TryGetCluster(KustoDatabaseEntry database, string environment, out string targetCluster)
        {
            if (database == null)
            {
                throw new ArgumentNullException();
            }

            targetCluster = null;

            try
            {
                if (_databaseMapping.Columns.Contains(environment))
                {
                    DataRow row = null;

                    foreach (DataRow r in _databaseMapping.Rows)
                    {
                        if (r.ItemArray.Any(s => s.ToString().Equals(database.Value, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            row = r;
                            break;
                        }
                    }

                    var clusterIndex = _databaseMapping.Rows.IndexOf(row);
                    if (clusterIndex <= (_clusterMapping.Rows.Count - 1))
                    {
                        DataRow clusterRow = _clusterMapping.Rows[clusterIndex];

                        if (clusterRow.Table.Columns.Contains(environment))
                        {
                            if (!clusterRow.IsNull(environment))
                            {
                                targetCluster = clusterRow[environment].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //log exception
            }

            return !string.IsNullOrWhiteSpace(targetCluster);
        }

        public bool TryGetDatabase(KustoClusterEntry cluster, out string targetDatabase)
        {
            return TryGetDatabase(cluster, _cloudEnvironment, out targetDatabase);
        }

        public bool TryGetDatabase(KustoClusterEntry cluster, string environment, out string targetDatabase)
        {
            if (cluster == null)
            {
                throw new ArgumentNullException();
            }

            targetDatabase = null;

            try
            {
                if (_clusterMapping.Columns.Contains(environment))
                {
                    DataRow row = null;

                    foreach (DataRow r in _clusterMapping.Rows)
                    {
                        if (r.ItemArray.Any(s => s.ToString().Equals(cluster.Value, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            row = r;
                            break;
                        }
                    }

                    if (row != null)
                    {
                        var databaseIndex = _clusterMapping.Rows.IndexOf(row);
                        if (databaseIndex <= (_databaseMapping.Rows.Count - 1))
                        {
                            DataRow databaseRow = _databaseMapping.Rows[databaseIndex];

                            if (databaseRow.Table.Columns.Contains(environment))
                            {
                                if (!databaseRow.IsNull(environment))
                                {
                                    targetDatabase = databaseRow[environment].ToString();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //log exception
            }

            return !string.IsNullOrWhiteSpace(targetDatabase);
        }
    }
}
