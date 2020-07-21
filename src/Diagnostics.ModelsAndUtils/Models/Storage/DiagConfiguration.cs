using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAzure.Storage.Table;

namespace Diagnostics.ModelsAndUtils.Models.Storage
{
    public class DiagConfiguration: TableEntity
    {
        public DiagConfiguration()
        {

        }

        public DiagConfiguration(string entityType, string resourceProvider)
        {
            PartitionKey = entityType;
            RowKey = resourceProvider;
        }

        /// <summary>
        /// Kusto Cluster Mapping 
        /// </summary>
        public string KustoClusterMapping;

        /// <summary>
        /// Github Sha of the json file
        /// </summary>
        public string GithubSha;
    }
}
