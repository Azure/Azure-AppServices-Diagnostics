using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Diagnostics.DataProviders.DataProviderConfigurations;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;

namespace Diagnostics.DataProviders
{
    public class AppInsightsDataProvider : DiagnosticDataProvider, IDiagnosticDataProvider, IAppInsightsDataProvider
    {
        const string AppInsightsTagName = "hidden-related:diagnostics/applicationInsightsSettings";
        private readonly IAppInsightsClient _appInsightsClient;
        private AppInsightsDataProviderConfiguration _configuration;

        public AppInsightsDataProvider(OperationDataCache cache, AppInsightsDataProviderConfiguration configuration) : base(cache)
        {
            _configuration = configuration;
            _appInsightsClient = new AppInsightsClient(_configuration);
            Metadata = new DataProviderMetadata
            {
                ProviderName = "AppInsights"
            };
        }

        public Task<bool> SetAppInsightsKey(string appId, string apiKey)
        {
            _appInsightsClient.SetAppInsightsKey(appId, apiKey);
            return Task.FromResult(true);
        }

        public async Task<bool> SetAppInsightsKey(OperationContext<IResource> cxt)
        {
            bool keyFound = false;
            if (cxt.Resource is App app)
            {
                var tag = GetAppIdAndKeyFromAppSettingsTags(app.Tags);
                if (tag != null)
                {
                    keyFound = await SetAppInsightsKey(tag.AppId, DecryptString(tag.ApiKey));
                }
            }
            return keyFound;
        }

        private string DecryptString(string encryptedString)
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(encryptedString);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_configuration.EncryptionKey);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }

        private AppInsightsTag GetAppIdAndKeyFromAppSettingsTags(string tagsXml)
        {
            AppInsightsTag tag = null;
            var tagValue = GetTagValue(tagsXml, AppInsightsTagName);
            if (!string.IsNullOrWhiteSpace(tagValue))
            {
                tag = JsonConvert.DeserializeObject<AppInsightsTag>(tagValue);
            }
            return tag;
        }

        private string GetTagValue(string tagsXml, string key)
        {
            string tagValue = string.Empty;
            var xDocument = XDocument.Parse(tagsXml);
            XNamespace nsSys = "http://schemas.microsoft.com/2003/10/Serialization/Arrays";
            IEnumerable<XElement> tag = from el in xDocument.Elements(nsSys + "ArrayOfKeyValueOfstringstring").Elements()
                                        where (string)el.Element(nsSys + "Key") == key
                                        select el;

            if (tag != null && tag.Count() > 0)
            {
                var valueElement = tag.FirstOrDefault().Element(nsSys + "Value");
                if (valueElement != null)
                {
                    tagValue = valueElement.Value;
                }

            }
            return tagValue;
        }

        public async Task<DataTable> ExecuteAppInsightsQuery(string query, string operationName)
        {
            AddQueryInformationToMetadata(query, operationName);
            return await _appInsightsClient.ExecuteQueryAsync(query);
        }

        public async Task<DataTable> ExecuteAppInsightsQuery(string query)
        {
            return await ExecuteAppInsightsQuery(query, "");
        }

        private void AddQueryInformationToMetadata(string query, string operationName = "")
        {
            bool queryExists = Metadata.PropertyBag.Any(x => x.Key == "Query" &&
                                                            x.Value.GetType() == typeof(DataProviderMetadataQuery) &&
                                                            x.Value.CastTo<DataProviderMetadataQuery>().Text.Equals(query, StringComparison.OrdinalIgnoreCase));

            if (!queryExists)
            {
                Metadata.PropertyBag.Add(new KeyValuePair<string, object>("Query",
                    new DataProviderMetadataQuery()
                    {
                        Text = query,
                        Url = "https://docs.microsoft.com/en-us/azure/azure-monitor/log-query/log-query-overview",
                        OperationName = operationName
                    }
                ));
            }
        }

        public DataProviderMetadata GetMetadata()
        {
            return Metadata;
        }
    }

}
