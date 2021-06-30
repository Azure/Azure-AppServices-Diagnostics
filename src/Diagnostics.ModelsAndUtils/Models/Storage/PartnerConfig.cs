using Microsoft.WindowsAzure.Storage.Table;

namespace Diagnostics.ModelsAndUtils.Models.Storage
{
    public class PartnerConfig : TableEntity
    {
        public PartnerConfig()
        {

        }

        public PartnerConfig(string entityType, string ConfigType)
        {
            PartitionKey = entityType;
            RowKey = ConfigType;
        }

        public string DevOpsUrl { get; set; }

        public string RepoPath { get; set; }

        public string Resource { get; set; } 

        public string Project { get; set; }

        public string Repository { get; set; }
    }
}
