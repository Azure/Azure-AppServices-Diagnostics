using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public static class ResponseDetectorViewExtensions
    {
        public static DiagnosticData AddDetectorCollection(this Response response, List<string> detectorIds)
        {
            var detectorCollectionRendering = new DetectorCollectionRendering
            {
                DetectorIds = detectorIds,
            };
            return AddDetectorCollection(response, detectorCollectionRendering);
        }

        public static DiagnosticData AddDetectorCollection(this Response response, List<string> detectorIds, IDictionary<string, string> additionalParams)
        {
            var detectorCollectionRendering = new DetectorCollectionRendering()
            {
                DetectorIds = detectorIds,
                AdditionalParams = additionalParams != null ? JsonConvert.SerializeObject(additionalParams) : string.Empty,
            };
            return AddDetectorCollection(response, detectorCollectionRendering);
        }

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
