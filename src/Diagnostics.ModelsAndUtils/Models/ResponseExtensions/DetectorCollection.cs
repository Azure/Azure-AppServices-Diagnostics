using System;
using System.Collections.Generic;
using System.Text;

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

        public static DiagnosticData AddDetector(this Response response, string detectorId)
        {
            var diagnosticData = new DiagnosticData()
            {
                RenderingProperties = new DetectorCollectionRendering()
                {
                    DetectorIds = new string[] { detectorId }
                }
            };

            response.Dataset.Add(diagnosticData);

            return diagnosticData;
        }
    }
}
