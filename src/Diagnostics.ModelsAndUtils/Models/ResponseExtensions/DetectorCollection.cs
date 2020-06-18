using Newtonsoft.Json;
using System.Collections.Generic;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public static class ResponseDetectorViewExtensions
    {
        public static DiagnosticData AddDetectorCollection(this Response response, List<string> detectorIds)
        {
            return AddDetectorCollection(response, detectorIds, null);
        }

        public static DiagnosticData AddDetectorCollection(this Response response, List<string> detectorIds, IDictionary<string, string> additionalParams)
        {
            var diagnosticData = new DiagnosticData()
            {
                RenderingProperties = new DetectorCollectionRendering()
                {
                    DetectorIds = detectorIds,
                    AdditionalParams = additionalParams != null ? JsonConvert.SerializeObject(additionalParams) : string.Empty,
                }
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
