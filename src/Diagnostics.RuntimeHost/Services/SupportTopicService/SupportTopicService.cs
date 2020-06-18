using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Diagnostics.DataProviders;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Newtonsoft.Json;

namespace Diagnostics.RuntimeHost.Services
{
    public interface ISupportTopicService
    {
        Task<SupportTopicModel> GetSupportTopicFromString(string supportTopicString, DataProviderContext dataProviderContext);
    }

    public class SupportTopicModel
    {
        public string SupportTopicId { get; set; }
        public string ProductId { get; set; }
        public string SupportTopicPath { get; set; }

        public SupportTopicModel(DataRow row)
        {
            SupportTopicId = row["SupportTopicId"].ToString();
            ProductId = row["ProductId"].ToString();
            SupportTopicPath = row["SupportTopicPath"].ToString();
        }
    }

    public class SupportTopicService : ISupportTopicService
    {
        private Dictionary<string, SupportTopicModel> _supportTopicCache;
        
        public SupportTopicService()
        {
            _supportTopicCache = new Dictionary<string, SupportTopicModel>();
        }

        private string GetSupportTopicKustoQuery() {
            string query = $@"cluster('azsupport').database('AzureSupportability').ActiveSupportTopicTree
                            | where Timestamp > ago(3d)
                            | extend SupportTopicId = iff(SupportTopicL3Id != '' and SupportTopicL3Id != ' ', SupportTopicL3Id, SupportTopicL2Id) 
                            | where SupportTopicId != ''
                            | summarize by ProductId, SupportTopicId, ProductName, SupportTopicL2Name, SupportTopicL3Name                            
                            | extend SupportTopicPath = iff(SupportTopicL3Name != '' and SupportTopicL3Name != ' ', strcat(ProductName, '\\', SupportTopicL2Name,'\\', SupportTopicL3Name), strcat(ProductName, '\\', SupportTopicL2Name))
                            | project ProductId, SupportTopicId, SupportTopicPath";

            return query;
        }

        private async Task PopulateSupportTopicCache(DataProviderContext dataProviderContext) {
            var dp = new DataProviders.DataProviders(dataProviderContext);
            DataTable supportTopicMappingTable = new DataTable();
            Guid requestIdGuid = Guid.NewGuid();
            supportTopicMappingTable = await dp.Kusto.ExecuteClusterQuery(GetSupportTopicKustoQuery(), requestIdGuid.ToString(), operationName: "PopulateSupportTopicCache");
            foreach(DataRow row in supportTopicMappingTable.Rows)
            {
                _supportTopicCache[row["SupportTopicPath"].ToString()] = new SupportTopicModel(row);
            }
        }

        public async Task<SupportTopicModel> GetSupportTopicFromString(string supportTopicString, DataProviderContext dataProviderContext)
        {
            SupportTopicModel result = null;
            if (!_supportTopicCache.TryGetValue(supportTopicString, out result))
            {
                await PopulateSupportTopicCache(dataProviderContext);
                _supportTopicCache.TryGetValue(supportTopicString, out result);
            }
            return result;
        }
    }
}
