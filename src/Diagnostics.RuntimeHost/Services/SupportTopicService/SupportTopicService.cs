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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Linq;

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
        private IConfiguration _configuration;

        public SupportTopicService(IConfiguration configuration)
        {
            _supportTopicCache = new Dictionary<string, SupportTopicModel>();
            _configuration = configuration;
        }

        private string GetSupportTopicKustoQuery() {
            string query = $@"ActiveSupportTopicTree
                            | where Timestamp > ago(3d)
                            | extend SupportTopicId = iff(SupportTopicL3Id != '' and SupportTopicL3Id != ' ', SupportTopicL3Id, SupportTopicL2Id) 
                            | where SupportTopicId != ''
                            | summarize by ProductId, SupportTopicId, ProductName, SupportTopicL2Name, SupportTopicL3Name                            
                            | extend SupportTopicPath = iff(SupportTopicL3Name != '' and SupportTopicL3Name != ' ', strcat(ProductName, '\\', SupportTopicL2Name,'\\', SupportTopicL3Name), strcat(ProductName, '\\', SupportTopicL2Name))
                            | project ProductId, SupportTopicId, SupportTopicPath";

            return query;
        }

        private async Task<DataTable> GetSupportTopicList(DataProviderContext dataProviderContext)
        {
            if (!_configuration.IsPublicAzure())
            {
                if (_configuration.GetSection("SupportTopicMap").GetChildren().Count() == 0)
                {
                    throw new Exception("Support topic map is empty.");
                }

                DataTable supportTopicTable = new DataTable();
                supportTopicTable.Columns.Add("SupportTopicId", typeof(string));
                supportTopicTable.Columns.Add("ProductId", typeof(string));
                supportTopicTable.Columns.Add("SupportTopicPath", typeof(string));
                
                var supportTopicList = _configuration.GetSection("SupportTopicMap").GetChildren();
                if(supportTopicList.Any() == true)
                {
                    foreach(var supportTopicEntry in supportTopicList)
                    {
                        string supportTopicId = supportTopicEntry.GetSection("SupportTopicId").Value;
                        string productId = supportTopicEntry.GetSection("ProductId").Value;
                        string supportTopicPath = supportTopicEntry.GetSection("supportTopicPath").Value;
                        if(!string.IsNullOrWhiteSpace(supportTopicId) && !string.IsNullOrWhiteSpace(productId) && !string.IsNullOrWhiteSpace(supportTopicPath) )
                        {
                            supportTopicTable.Rows.Add(supportTopicId, productId, supportTopicPath);
                        }
                    }
                }

                return supportTopicTable;
            }
            else
            {
                Guid requestIdGuid = Guid.NewGuid();
                var dp = new DataProviders.DataProviders(dataProviderContext);
                return await dp.Kusto.ExecuteClusterQuery(GetSupportTopicKustoQuery(), "azsupportfollower.westus2", "AzureSupportability", requestIdGuid.ToString(), operationName: "PopulateSupportTopicCache");
            }
        }

        

        private async Task PopulateSupportTopicCache(DataProviderContext dataProviderContext) 
        {            
            DataTable supportTopicMappingTable = new DataTable();            
            supportTopicMappingTable = await GetSupportTopicList(dataProviderContext);

            foreach (DataRow row in supportTopicMappingTable.Rows)
            {
                _supportTopicCache[getSupportTopicCacheKey(row["SupportTopicPath"].ToString())] = new SupportTopicModel(row);
            }
        }       

        private string getSupportTopicCacheKey(string supportTopicString)
        {
            if(string.IsNullOrWhiteSpace(supportTopicString))
            {
                return string.Empty;
            }
            return supportTopicString.Replace(@"\", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<SupportTopicModel> GetSupportTopicFromString(string supportTopicString, DataProviderContext dataProviderContext)
        {
            SupportTopicModel result = null;
            if (!_supportTopicCache.TryGetValue(getSupportTopicCacheKey(supportTopicString), out result))
            {
                await PopulateSupportTopicCache(dataProviderContext);
                _supportTopicCache.TryGetValue(getSupportTopicCacheKey(supportTopicString), out result);
            }
            return result;
        }
    }
}
