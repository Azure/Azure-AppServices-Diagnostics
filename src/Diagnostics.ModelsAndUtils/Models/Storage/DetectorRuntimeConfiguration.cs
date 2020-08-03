using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAzure.Storage.Table;

namespace Diagnostics.ModelsAndUtils.Models.Storage
{
    public class DetectorRuntimeConfiguration: TableEntity
    {
        public DetectorRuntimeConfiguration()
        {

        }

        public DetectorRuntimeConfiguration(string entityType, string resourceProvider)
        {
            PartitionKey = entityType;
            RowKey = resourceProvider;
        }

        /// <summary>
        /// Kusto Cluster Mapping 
        /// </summary>
        public string KustoClusterMapping { get; set; } = string.Empty;

        /// <summary>
        /// Github Sha of the json file
        /// </summary>
        public string GithubSha { get; set; } = string.Empty;

        /// <summary>
        /// Flag to indicate if config is disabled
        /// </summary>
        public bool IsDisabled { get; set; } = false;
    }
}
