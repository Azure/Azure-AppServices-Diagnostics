using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Diagnostics.DataProviders
{
    public class KustoQuery
    {
        public string Text;        
        public string Url;
    }
    public class KustoDataProvider: DiagnosticDataProvider, IDiagnosticDataProvider
    {
        private KustoDataProviderConfiguration _configuration;
        private IKustoClient _kustoClient;

        public KustoDataProvider(OperationDataCache cache, KustoDataProviderConfiguration configuration) : base(cache)
        {
            _configuration = configuration;
            _kustoClient = KustoClientFactory.GetKustoClient(configuration);
            Metadata = new DataProviderMetadata
            {
                ProviderName = "Kusto"
            };
        }

        public async Task<DataTable> ExecuteQuery(string query, string stampName, string requestId = null, string operationName = null)
        {
            AddQueryInformationToMetadata(query, stampName);
            return await _kustoClient.ExecuteQueryAsync(query, stampName, requestId, operationName);
        }

        private void AddQueryInformationToMetadata(string query, string stampName)
        {
            KustoQuery q = new KustoQuery
            {
                Text = query,               
                Url = CreateKustoQueryUri(stampName, query)
            };

            Metadata.PropertyBag.Add(new KeyValuePair<string, object>("Query", q));
        }

        private string ParseRegionFromStamp(string stampName)
        {
            if (string.IsNullOrWhiteSpace(stampName))
            {
                throw new ArgumentNullException("stampName");
            }

            var stampParts = stampName.Split(new char[] { '-' });
            if (stampParts.Any() && stampParts.Length >= 3)
            {
                return stampParts[2];
            }

            //return * for private stamps if no prod stamps are found
            return "*";
        }

        private string CreateKustoQueryUri(string stampName, string query)
        {
            string kustoClusterName = null;
            try
            {
                string appserviceRegion = ParseRegionFromStamp(stampName);

                if (!_configuration.RegionSpecificClusterNameCollection.TryGetValue(appserviceRegion.ToLower(), out kustoClusterName))
                {
                    if (!_configuration.RegionSpecificClusterNameCollection.TryGetValue("*", out kustoClusterName))
                    {
                        throw new KeyNotFoundException(String.Format("Kusto Cluster Name not found for Region : {0}", appserviceRegion.ToLower()));
                    }
                }

                string encodedQuery = EncodeQueryAsBase64Url(query);

                var url = string.Format("https://web.kusto.windows.net/clusters/{0}.kusto.windows.net/databases/{1}?q={2}", kustoClusterName, _configuration.DBName, encodedQuery);

                return url;
            }
            catch (Exception ex)
            {
                string message = string.Format("stamp : {0}, kustoClusterName : {1}, Exception : {2}",
                    stampName ?? "null",
                    kustoClusterName ?? "null",
                    ex.ToString());
                throw;
            }
        }

        // From Kusto.Data.Common.CslCommandGenerator.EncodeQueryAsBase64Url
        private string EncodeQueryAsBase64Url(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return string.Empty;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(query);
            string result;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                {
                    gZipStream.Write(bytes, 0, bytes.Length);
                }
                memoryStream.Seek(0L, SeekOrigin.Begin);
                result = HttpUtility.UrlEncode(Convert.ToBase64String(memoryStream.ToArray()));
            }
            return result;
        }
    }
}
