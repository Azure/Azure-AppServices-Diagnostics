using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Diagnostics.RuntimeHost.Services.CacheService;
using System.Net.Http.Headers;
using Diagnostics.ModelsAndUtils.Models;
using System.Data;
using Microsoft.CodeAnalysis;
using Diagnostics.ModelsAndUtils.Attributes;

namespace Diagnostics.RuntimeHost.Services.DiagnosticsTranslator
{
    public interface IDiagnosticTranslatorService
    {
        Task<Response> GetResponseTranslations(Response response, string language);
        Task<IEnumerable<DiagnosticApiResponse>> GetMetadataTranslations(IEnumerable<DiagnosticApiResponse> listDetectorsResponse, string language);
    }

    public class DiagnosticTranslatorService : IDiagnosticTranslatorService
    {
        private IConfiguration _config;
        private static string _enableLocalization;
        private static string _translatorSubscriptionKey;
        private static string _translatorBaseURL;
        private static string _translatorApiURL;
        private static string _apiVersion;

        private ITranslationCacheService _translationCacheService;
        private static readonly HttpClient _httpClient = new HttpClient();

        private void InitializeHttpClient()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.BaseAddress = new Uri(_translatorBaseURL);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _translatorSubscriptionKey);
        }

        /// <summary>
        /// Creates a new instance of DiagnosticTranslatorService
        /// </summary>
        /// <param name="configuration">Configuration object</param>
        /// <param name="translationCacheService">Translation cache service</param>
        public DiagnosticTranslatorService(IConfiguration configuration, ITranslationCacheService translationCacheService)
        {
            _config = configuration;
            _enableLocalization = _config[$"DiagnosticTranslator:EnableLocalization"];
            _translatorSubscriptionKey = _config[$"DiagnosticTranslator:TranslatorSubscriptionKey"];
            _translatorBaseURL = _config[$"DiagnosticTranslator:BaseUri"];
            _translatorApiURL = _config[$"DiagnosticTranslator:TranslatorApiUri"];
            _apiVersion = _config[$"DiagnosticTranslator:ApiVersion"];

            this._translationCacheService = translationCacheService;
            InitializeHttpClient();
        }

        public async Task<Response> GetResponseTranslations(Response diagnosticResponse, string language)
        {
            if (diagnosticResponse == null || !IsLocalizationApplicable(language))
            {
                return diagnosticResponse;
            }

            List<Definition> metadataTranslations = await GetDetectorsDefinitionsTranslations(new List<Definition> { diagnosticResponse.Metadata }, language);
            diagnosticResponse.Metadata = metadataTranslations != null && metadataTranslations.Count > 0 ? metadataTranslations[0]: diagnosticResponse.Metadata;

            for (int j = 0; j < diagnosticResponse.Dataset.Count; j++)
            {
                RenderingType renderingType = diagnosticResponse.Dataset[j].RenderingProperties.Type;
                // Starting from Insights and data summary localizations for now. Also render title and description for the remaining rendering types.
                switch (renderingType)
                {
                    case RenderingType.Insights:
                        diagnosticResponse.Dataset[j] = await GetInsightsTranslation(diagnosticResponse.Dataset[j], language);
                        break;
                    case RenderingType.DataSummary:
                        diagnosticResponse.Dataset[j] = await GetDataSummaryTranslation(diagnosticResponse.Dataset[j], language);
                        break;
                    case RenderingType.DropDown:
                        diagnosticResponse.Dataset[j] = await GetDropdownTranslation(diagnosticResponse.Dataset[j], language);
                        break;
                    default:
                        diagnosticResponse.Dataset[j] = await GetBaseRenderingTranslation(diagnosticResponse.Dataset[j], language);
                        break;
                }
            }

            return diagnosticResponse;
        }

        public async Task<IEnumerable<DiagnosticApiResponse>> GetMetadataTranslations(IEnumerable<DiagnosticApiResponse> listDetectorsResponse, string language)
        {
            if (listDetectorsResponse == null || !IsLocalizationApplicable(language))
            {
                return listDetectorsResponse;
            }

            IEnumerable<DiagnosticApiResponse> translatedResponse = listDetectorsResponse;
      
            List<Definition> allDetectorsDefinitions = listDetectorsResponse.Select(detectorResponse => detectorResponse.Metadata).ToList<Definition>();
            List<Definition> metadataTranslations = await GetDetectorsDefinitionsTranslations(allDetectorsDefinitions, language);

            if (metadataTranslations != null && metadataTranslations.Count == translatedResponse.Count())
            {
                translatedResponse = translatedResponse.Select((response, i) => { response.Metadata = metadataTranslations[i]; return response; });
            }

            return translatedResponse;
        }

        #region Localization Helper Methods

        // Scenarios to enable localization:
        // 1. EnableLocalization is not set or set to be true in appsettings/keyvault
        // 2. Language to translate to is not empty or English
        private static bool IsLocalizationApplicable(string language)
        {
            return (string.IsNullOrWhiteSpace(_enableLocalization) || string.Compare(_enableLocalization, "true", StringComparison.OrdinalIgnoreCase) == 0) && !String.IsNullOrWhiteSpace(language) && string.Compare(language, "en", StringComparison.OrdinalIgnoreCase) != 0 && !language.StartsWith("en.", StringComparison.CurrentCulture);
        }

        private async Task<List<Definition>> GetDetectorsDefinitionsTranslations(List<Definition> metadataList, string language)
        {
            if (metadataList == null || metadataList.Count == 0)
            {
                return metadataList;
            }
            List<Definition> responseMetaDataList = metadataList;
            List<string> textsToTraslate = new List<string>();
            metadataList.ForEach((metadata) =>
            {
                textsToTraslate.AddRange(new List<string> { metadata.Name != null ? metadata.Name : "", metadata.Description != null ? metadata.Description : "" });
            });

            List<string> translatedText = await GetGroupTranslations(textsToTraslate, language);

            if (translatedText != null && translatedText.Count > 1 && translatedText.Count == metadataList.Count * 2)
            {
                for (int i = 0; i * 2 + 1 < translatedText.Count; i++)
                {
                    responseMetaDataList[i].Name = translatedText[i * 2];
                    responseMetaDataList[i].Description = translatedText[i * 2 + 1];
                }
            }
            return responseMetaDataList;
        }

        /// <summary>
        /// Get localization result for each rendering type.
        /// </summary>
        /// <param name="dataset">Dataset object to be localized.</param>
        /// <param name="language">Language to translate to.</param>
        /// <param name="allowedColumnIndexList">Column indexes in dataset table which is of string type and needs to be localized.</param>
        /// <returns>Localized dataset object</returns>
        private async Task<DiagnosticData> GetBaseRenderingTranslation(DiagnosticData dataset, string language, List<int> allowedColumnIndexList = null)
        {
            List<string> renderingPropertiesToTranslate = new List<string>();
            List<string> textsToTraslate = new List<string>();

            // Localize name and description for general rendering types. This will take care of rendering container localizations in UI.
            if (dataset.RenderingProperties != null)
            {
                string title = string.IsNullOrWhiteSpace(dataset.RenderingProperties.Title) ? "" : dataset.RenderingProperties.Title;
                string description = string.IsNullOrWhiteSpace(dataset.RenderingProperties.Description) ? "" : dataset.RenderingProperties.Description;
                renderingPropertiesToTranslate.AddRange(new List<string> { title, description });

                List<string> translatedProperties = await GetTranslations(renderingPropertiesToTranslate, language).ConfigureAwait(false);

                if (translatedProperties != null && translatedProperties.Count > 1)
                {
                    dataset.RenderingProperties.Title = translatedProperties[0].ToString();
                    dataset.RenderingProperties.Description = translatedProperties[1].ToString();
                }
            }

            // Localize dataset table content strings
            if (dataset.Table != null && dataset.Table.Rows != null && allowedColumnIndexList != null)
            {
                int rowCount = dataset.Table.Rows.Count;
                int columnCount = dataset.Table.Columns.Count;
                int allowedColumnCount = allowedColumnIndexList.Count;
                
                foreach(int allowedIndex in allowedColumnIndexList)
                {
                    if (allowedIndex > columnCount)
                    {
                        throw new IndexOutOfRangeException(string.Format("Column Index {0} to be translated is out of datatable columns range {1}", allowedIndex, columnCount));
                    }
                }

                for (int i = 0; i < rowCount; i++)
                {
                    for (int j = 0; j < allowedColumnCount && allowedColumnIndexList[j] < columnCount; j++)
                    {
                        int columnIndex = allowedColumnIndexList[j];
                        string text = dataset.Table.Rows[i][columnIndex].ToString().Equals("null", StringComparison.OrdinalIgnoreCase) ? "" : dataset.Table.Rows[i][columnIndex].ToString();
                        textsToTraslate.Add(text);
                    }
                }

                List<string> translatedText = await GetGroupTranslations(textsToTraslate, language).ConfigureAwait(false);
                if (translatedText.Count != rowCount * allowedColumnCount)
                {
                    //If something goes wrong and we don't get the same count of translated string result, return the original dataset
                    return dataset;
                }

                for (int i = 0; i < rowCount; i++)
                {
                    for (int j = 0; j < allowedColumnIndexList.Count && allowedColumnIndexList[j] < columnCount; j++)
                    {
                        int columnIndex = allowedColumnIndexList[j];
                        dataset.Table.Rows[i][columnIndex] = translatedText[i * allowedColumnCount + j];
              
                    }
                }
            }
            return dataset;
        }

        public async Task<DiagnosticData> GetDataSummaryTranslation(DiagnosticData dataset, string language)
        {
            int nameColumnIndex = 0;
            return await GetBaseRenderingTranslation(dataset, language, new List<int> { nameColumnIndex });
        }

        public async Task<DiagnosticData> GetInsightsTranslation(DiagnosticData dataset, string language)
        {
            int messageColumnIndex = 1;
            int insightNameColumnIndex = 2;
            int insightValueColumnIndex = 3;
            int insightSolutionColumnIndex = 5;

            return await GetBaseRenderingTranslation(dataset, language, new List<int> { messageColumnIndex, insightNameColumnIndex, insightValueColumnIndex, insightSolutionColumnIndex });
        }

        public async Task<DiagnosticData> GetDropdownTranslation(DiagnosticData dataset, string language)
        {
            int dropdownLabelColumn = 0;
            int dropdownKeyColumn = 1;

            return await GetBaseRenderingTranslation(dataset, language, new List<int> { dropdownLabelColumn, dropdownKeyColumn });
        }

        // Cognitive translation API has a limit of 100 elements for string arrays to be sent in request body. 
        private async Task<List<string>> GetGroupTranslations(List<string> textsToTranslate, string languageToTranslate)
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

        // Translation method to make the http request with string arrays to be translated
        public async Task<List<string>> GetTranslations(List<string> textsToTranslate, string languageToTranslate)
        {
            if (textsToTranslate == null)
            {
                return null;
            }

            // Always look for cache before calling translator API
            Tuple<string, string> key = new Tuple<string, string>(languageToTranslate, string.Join("_", textsToTranslate));
            if (this._translationCacheService.TryGetValue(key, out List<string> value))
            {
                return value;
            }

            string route = string.Format(_translatorApiURL, _apiVersion, languageToTranslate);
            object[] body = textsToTranslate.Select((text) => { return new { Text = text }; }).ToArray();
            var requestBody = JsonConvert.SerializeObject(body);

            try
            {
                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(_translatorBaseURL + route);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.SendAsync(request);
                    string result = await response.Content.ReadAsStringAsync();
                    JArray jObjectResponse = JArray.Parse(result);

                    List<string> translatedTexts = new List<string>();
                    foreach (var responseObject in jObjectResponse)
                    {
                        if (responseObject["translations"] != null && responseObject["translations"].Count() > 0)
                        {
                            translatedTexts.Add(responseObject["translations"][0]["text"].ToString());
                        }
                    }

                    // Update cache before returning translated texts array
                    this._translationCacheService.AddOrUpdate(key, translatedTexts);
                    return translatedTexts;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion 
    }
}
