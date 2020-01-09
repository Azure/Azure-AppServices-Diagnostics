using Newtonsoft.Json;
using System.Collections.Generic;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public static class ResponseDetectorViewExtensions
    {
        public static DiagnosticData AddDetectorCollection(this Response response, List<string> detectorIds)
        {
            var diagnosticData = new DiagnosticData()
            {
                RenderingProperties = new DetectorCollectionRendering()
                {
                    DetectorIds = detectorIds
                }
            };

            response.Dataset.Add(diagnosticData);

            return diagnosticData;
        }

        public static DiagnosticData AddDetector(this Response response, string detectorId, IDictionary<string, string> additionalParams = null )
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
