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
                _clusterMapping.Columns.Add(env, typeof(string));
            }

            foreach (IEnumerable<KeyValuePair<string,string>> bodyOfKeyValuePairs in existingMap)
            {
                var clusterRow = _clusterMapping.NewRow();
                var databaseRow = _databaseMapping.NewRow();

                foreach (KeyValuePair<string, string> pair in bodyOfKeyValuePairs)
                {
                    foreach (DataColumn column in _clusterMapping.Columns)
                    {
                        if (pair.Key.ToLower().Contains(column.ColumnName.ToLower()))
                        {
                            if (pair.Key.ToLower().Contains("clustername"))
                            {
                                clusterRow[column] = pair.Value;
                            }
                            else if (pair.Key.ToLower().Contains("databasename"))
                            {
                                databaseRow[column] = pair.Value;
                            }
                        }
                    }
                }
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
                        break;
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

                return sourceRow[_cloudEnvironment].ToString();
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
                        break;
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

                return sourceRow[_cloudEnvironment].ToString();
            }

            return null;
        }

        public void Dispose()
        {
            _clusterMapping.Dispose();
            _databaseMapping.Dispose();
        }
    }
}
