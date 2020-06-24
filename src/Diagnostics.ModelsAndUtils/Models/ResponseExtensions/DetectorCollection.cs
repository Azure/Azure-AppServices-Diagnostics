using Newtonsoft.Json;
using System.Collections.Generic;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public static class ResponseDetectorViewExtensions
    {
        /// <summary>
        /// Add list of child detectors by passing detector Ids
        /// </summary>
        /// <param name="response">Response</param>
        /// <param name="detectorIds">List<![CDATA[<string>]]>,list of detector Ids</param>
        /// <returns></returns>
        public static DiagnosticData AddDetectorCollection(this Response response, List<string> detectorIds)
        {
            var detectorCollectionRendering = new DetectorCollectionRendering
            {
                DetectorIds = detectorIds,
            };
            return AddDetectorCollection(response, detectorCollectionRendering);
        }

        /// <summary>
        /// Add list of child detectors by passing detector Ids and additional parameters 
        /// </summary>
        /// <param name="response">Response</param>
        /// <param name="detectorIds">List<![CDATA[<string>]]>,list of detector Ids</param>
        /// <param name="additionalParams">Dictionary<![CDATA[<string,string>]]>,additionalParams will append into Url query string</param>
        /// <returns></returns>
        public static DiagnosticData AddDetectorCollection(this Response response, List<string> detectorIds, IDictionary<string, string> additionalParams)
        {
            var detectorCollectionRendering = new DetectorCollectionRendering()
            {
                DetectorIds = detectorIds,
                AdditionalParams = additionalParams != null ? JsonConvert.SerializeObject(additionalParams) : string.Empty,
            };
            return AddDetectorCollection(response, detectorCollectionRendering);
        }

        /// <summary>
        /// Add list of child detectors from depended resource, by passing detector Ids and depended resource Uri, also accept additional parameters 
        /// </summary>
        /// <param name="response">Response</param>
        /// <param name="detectorIds">List<![CDATA[<string>]]>,list of detector Ids</param>
        /// <param name="resourceUri">depended resource Uri</param>
        /// <param name="additionalParams">Dictionary<![CDATA[<string,string>]]>,additionalParams will append into Url query string</param>
        /// <returns></returns>
        public static DiagnosticData AddDetectorCollection(this Response response,List<string> detectorIds,string resourceUri,IDictionary<string,string> additionalParams = null)
        {
            var detectorCollectionRendering = new DetectorCollectionRendering()
            {
                DetectorIds = detectorIds,
                ResourceUri = resourceUri,
                AdditionalParams = additionalParams != null ? JsonConvert.SerializeObject(additionalParams) : string.Empty,
            };
            return AddDetectorCollection(response, detectorCollectionRendering);
        }

        /// <summary>
        /// Add list of child detectors by passing DetectorCollectionRendering class
        /// </summary>
        /// <param name="response">Response</param>
        /// <param name="detectorCollectionRendering">DetectorCollectionRendering</param>
        /// <returns></returns>
        public static DiagnosticData AddDetectorCollection(this Response response, DetectorCollectionRendering detectorCollectionRendering)
        {
            var diagnosticData = new DiagnosticData()
            {
                RenderingProperties = detectorCollectionRendering
            };

            response.Dataset.Add(diagnosticData);

            return diagnosticData;
        }


        public static DiagnosticData AddDetector(this Response response, string detectorId)
        {
            return AddDetector(response, detectorId, null);
        }

        public static DiagnosticData AddDetector(this Response response, string detectorId, IDictionary<string, string> additionalParams)
        {
            var diagnosticData = new DiagnosticData()
            {
                RenderingProperties = new DetectorCollectionRendering()
                {
                    DetectorIds = new string[] { detectorId },
                    AdditionalParams = additionalParams != null ? JsonConvert.SerializeObject(additionalParams) : string.Empty,
                }
            };

            response.Dataset.Add(diagnosticData);

            return diagnosticData;
        }
    }
}
