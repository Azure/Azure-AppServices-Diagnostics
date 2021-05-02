using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Diagnostics.RuntimeHost.Services.CacheService;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Diagnostics.ModelsAndUtils.Models;
using System.Data;
using Microsoft.CodeAnalysis;
using Diagnostics.ModelsAndUtils.Attributes;

namespace Diagnostics.RuntimeHost.Services.DiagnosticsTranslator
{
    public interface IDiagnosticTranslatorService
    {
        Task<List<string>> GetTranslations(List<string> textToTranslate, string locale);
        Task<Response> GetResponseTranslations(Response response, string language);
        Task<IEnumerable<DiagnosticApiResponse>> GetMetadataTranslations(IEnumerable<DiagnosticApiResponse> listDetectorsResponse, string language);
    }


   // this._diagnosticTranslator.GetResponseTranslations(diagnosticResponse, locale);

    public class DiagnosticTranslatorService : IDiagnosticTranslatorService
    {
        private IHostingEnvironment _env;
        private IConfiguration _config;
        private readonly bool isEnabled;

        private static string _translatorSubscriptionKey;
        private static readonly string translatorEndpoint = "https://api.cognitive.microsofttranslator.com/";

        private ITranslationCacheService _translationCacheService;
        //private IDictionary<string, ITranslationCacheService> TranslationCache { get; }

        private ConcurrentDictionary<string, string> cache;
        int cacheExpirationTimeInSecs = 60;
        private static readonly HttpClient _httpClient = new HttpClient();

        //public HttpClient Client => _client;
        //public Uri BaseUri => _client.BaseAddress;

        private void InitializeHttpClient()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.BaseAddress = new Uri(translatorEndpoint);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _translatorSubscriptionKey);
        }


        /// <summary>
        /// Creates a new instance of DiagnosticTranslatorService
        /// </summary>
        /// <param name="configuration">Configuration object</param>
        /// <param name="environment">Environment object</param>
        public DiagnosticTranslatorService(IConfiguration configuration, IHostingEnvironment environment, ITranslationCacheService translationCacheService)
        {
            _config = configuration ;
            _env = environment;
            _translatorSubscriptionKey = _config[$"DiagnosticTranslator:TranslatorSubscriptionKey"];
            Console.Write("key: " + _translatorSubscriptionKey);
            this._translationCacheService = translationCacheService;
            InitializeHttpClient();
        }

        private async Task<List<Definition>> GetMetadataTranslations(List<Definition> metadataList, string language)
        {
            if (metadataList == null || metadataList.Count == 0)
            {
                return metadataList;
            }
            List<Definition> responseMetaDataList = metadataList;
            List<string> textsToTraslate = new List<string>();
            metadataList.ForEach((metadata) =>
            {
                textsToTraslate.AddRange(new List<string> { metadata.Name, metadata.Description });
            });
            
            List<string> translatedText = await GetGroupTranslations(textsToTraslate, language);
            
            if (translatedText != null && translatedText.Count > 1 && translatedText.Count == metadataList.Count*2)
            {
                for (int i = 0; i*2 +1 < translatedText.Count; i++)
                {
                    responseMetaDataList[i].Name = translatedText[i * 2];
                    responseMetaDataList[i].Description = translatedText[i * 2 + 1];
                }
            }
            return responseMetaDataList;
        }

        public async Task<Response> GetResponseTranslations(Response diagnosticResponse, string language)
        {
            if (diagnosticResponse == null)
            {
                return diagnosticResponse;
            }

            List<Definition> metadataTranslations = await GetMetadataTranslations(new List<Definition> { diagnosticResponse.Metadata }, language);
            diagnosticResponse.Metadata = metadataTranslations != null && metadataTranslations.Count > 1 ? metadataTranslations[0]: diagnosticResponse.Metadata;

            for (int j = 0; j < diagnosticResponse.Dataset.Count; j++)
            {
                RenderingType renderingType = diagnosticResponse.Dataset[j].RenderingProperties.Type;
                switch (renderingType)
                {
                    case RenderingType.Insights:
                        diagnosticResponse.Dataset[j] = await GetInsightsTranslation(diagnosticResponse.Dataset[j], language);
                        Console.WriteLine("Modifying dataset, {0}", JsonConvert.SerializeObject(diagnosticResponse.Dataset[j]));
                        break;
                    default:
                        break;
                }
            }

            return diagnosticResponse;
        }

        public async Task<IEnumerable<DiagnosticApiResponse>> GetMetadataTranslations(IEnumerable<DiagnosticApiResponse> listDetectorsResponse, string language)
        {
            IEnumerable<DiagnosticApiResponse> translatedResponse = listDetectorsResponse;
      
            List<Definition> allDetectorsDefinitions = listDetectorsResponse.Select(detectorResponse => detectorResponse.Metadata).ToList<Definition>();
            List<Definition> metadataTranslations = await GetMetadataTranslations(allDetectorsDefinitions, language);

            if (metadataTranslations != null && metadataTranslations.Count == translatedResponse.Count())
            {
                translatedResponse = translatedResponse.Select((response, i) => { response.Metadata = metadataTranslations[i]; return response; });
            }

            return translatedResponse;
        }

        //public async Task<DiagnosticData> getDetectorListTranslations(DiagnosticData dataset, string language)
        //{

        //}

        //
        //new DataColumn("Status", typeof(string)),
        //        new DataColumn("Message", typeof(string)),
        //        new DataColumn("Data.Name", typeof(string)),
        //        new DataColumn("Data.Value", typeof(string)),
        //        new DataColumn("Expanded", typeof(string)),
        //        new DataColumn(nameof(Insight.Solutions), typeof(string))

        public async Task<DiagnosticData> GetInsightsTranslation(DiagnosticData dataset, string language)
        {
            List<string> textsToTraslate = new List<string>();
            List<string> textsTraslated = new List<string>();
            try
            {
                if (dataset.Table != null && dataset.Table.Rows != null)
                {
                    int rowCount = dataset.Table.Rows.Count;
                    int columnCount = dataset.Table.Columns.Count;
                    int statusColumnIndex = 0;
                    int expandedColumnIndex = 4;

                    foreach (DataRow row in dataset.Table.Rows)
                    {
                        foreach (DataColumn column in dataset.Table.Columns)
                        {
                            string originalText = row[column].ToString().Equals( "null", StringComparison.OrdinalIgnoreCase) ? "" : row[column].ToString();               
                            textsToTraslate.Add(originalText);
                        }
                    }

                    List<string> translatedText = await GetTranslations(textsToTraslate, language);

                    if (translatedText.Count != rowCount * columnCount)
                    {
                        //Make sure we get the same count of translated value mapping 
                        Console.WriteLine("dateset number not matching {0} vs {1}", translatedText.Count, rowCount * columnCount);
                        return dataset;
                    }

                    // For "Status" and "Expanded" column, we don't need translated value 
                    for (int i = 0; i < translatedText.Count; i++)
                    {
                        int rowIndex = i / columnCount;
                        int columnIndex = i % columnCount;

                        if (columnIndex != statusColumnIndex && columnIndex != expandedColumnIndex)
                        {
                            dataset.Table.Rows[rowIndex][columnIndex] = translatedText[i];
                        }                 
                    }

                    Console.WriteLine("get the table");
                    Console.WriteLine(JsonConvert.SerializeObject(dataset.Table));

                    return dataset;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return dataset;
        }

        public async Task<List<string>> GetGroupTranslations(List<string> textsToTranslate, string languageToTranslate)
        {
            int translationGroupCount = textsToTranslate.Count / 100;
            int lastGroupSize = textsToTranslate.Count % 100;
            List<List<string>> translationGroupList = new List<List<string>>();
            for (int i = 0; i <= translationGroupCount; i++)
            {
                int groupSize = i == translationGroupCount ? lastGroupSize : 100;
                translationGroupList.Add(textsToTranslate.GetRange(i * 100, groupSize));
            }

            var translatedTextsGroup = await Task.WhenAll(translationGroupList.Select(textBatch => GetTranslations(textBatch, languageToTranslate)));
            return translatedTextsGroup.Where(group => group != null).SelectMany(group => group).ToList();
        }

        /// <summary>
        /// Translator string texts to a specific language
        /// If the Attribute doesn't exists, returns Null
        /// </summary>
        /// <param name="element">XElement</param>
        /// <param name="attributeName">Name of the attribute</param>
        /// <returns>Attribute value</returns>
        public async Task<List<string>> GetTranslations(List<string> textsToTranslate, string languageToTranslate)
        {
            // Input and output languages are defined as parameters.
            string route = $"/translate?api-version=3.0&from=en&to={languageToTranslate}";
            List<Object> texts = new List<Object>();
            foreach (string text in textsToTranslate)
            {
                texts.Add(new { Text = text });
            }

            object[] body = texts.ToArray();
            var requestBody = JsonConvert.SerializeObject(body);

            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(translatorEndpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                //request.Headers.Add("Ocp-Apim-Subscription-Key", translatorSubscriptionKey);
                // request.Headers.Add("Ocp-Apim-Subscription-Region", location);

                // Send the request and get response.
                HttpResponseMessage response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                // Read response as a string.

                string result = await response.Content.ReadAsStringAsync();
                JArray jObjectResponse = JArray.Parse(result);
                //var translationObject = JObject.Parse(result);

                List<string> translatedTexts = new List<string>();
                foreach(var responseObject in jObjectResponse)
                {
                    if (responseObject["translations"] != null && responseObject["translations"].Count() > 0)
                    {
                        translatedTexts.Add(responseObject["translations"][0]["text"].ToString());
                    }
                }

                Console.WriteLine(JsonConvert.SerializeObject(translatedTexts));
                return translatedTexts;
            }
        }
    }
}
