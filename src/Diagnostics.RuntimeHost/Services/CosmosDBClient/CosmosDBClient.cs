using Diagnostics.RuntimeHost.Models.CosmosModels;
using Microsoft.Extensions.Configuration;

namespace Diagnostics.RuntimeHost.Services.CosmosDBClient
{
    public class CosmosDBClient
    {
        private readonly string Endpoint;
        private readonly string Key;
        private readonly string DatabaseId;

        public DocumentDBRepository<DetectorData> DetectorDataTable { get; set; }
        public DocumentDBRepository<ScriptTextTable> ScriptTextTable { get; set; }
        public DocumentDBRepository<SupportTopic> SupportTopicTable { get; set; }

        public CosmosDBClient(IConfiguration config)
        {
            Endpoint = config["SourceWatcher:CosmosDB:Endpoint"];
            Key = config["SourceWatcher:CosmosDB:Key"];
            DatabaseId = config["SourceWatcher:CosmosDB:DatabaseId"];
            //Initalize database connection
            DetectorDataTable = new DocumentDBRepository<DetectorData>("DetectorDataTable", Endpoint, Key, DatabaseId);
            ScriptTextTable = new DocumentDBRepository<ScriptTextTable>("ScriptTextTable", Endpoint, Key, DatabaseId);
            SupportTopicTable = new DocumentDBRepository<SupportTopic>("SupportTopicTable", Endpoint, Key, DatabaseId);
        }
    }
}
