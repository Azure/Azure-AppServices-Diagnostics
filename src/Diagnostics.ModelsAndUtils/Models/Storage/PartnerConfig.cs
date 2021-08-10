using Microsoft.WindowsAzure.Storage.Table;

namespace Diagnostics.ModelsAndUtils.Models.Storage
{
    public class PartnerConfig 
    {
        public string DevOpsUrl { get; set; }

        public string FolderPath { get; set; }

        public string ResourceProvider { get; set; } 

        public string Project { get; set; }

        public string Repository { get; set; }
    }
}
