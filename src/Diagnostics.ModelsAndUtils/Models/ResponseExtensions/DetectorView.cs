using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public static class ResponseDetectorViewExtensions
    {
        public static DiagnosticData AddDetectorView(this Response response, List<string> detectorIds)
        {
            var diagnosticData = new DiagnosticData()
            {
                RenderingProperties = new DetectorRendering()
                {
                    DetectorIds = detectorIds
                }
            };

            response.Dataset.Add(diagnosticData);

            return diagnosticData;
        }
    }
}
