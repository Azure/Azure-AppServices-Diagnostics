using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Models;
using Newtonsoft.Json;

namespace Diagnostics.RuntimeHost.Utilities
{
    internal class InsightsAPIHelpers
    {
        public static BodyValidationResult ValidateQueryBody(DiagnosticReportQuery queryBody)
        {
            var result = new BodyValidationResult();
            if (queryBody == null)
            {
                result.Message = "Body cannot be null.";
                result.Status = false;
                return result;
            }
            if (queryBody.Detectors != null && queryBody.Detectors.Count > 0)
            {
                result.Status = true;
                return result;
            }
            if (queryBody.SupportTopicId != null && queryBody.SupportTopicId.Length > 0)
            {
                result.Status = true;
                return result;
            }
            if (queryBody.Text != null && queryBody.Text.Length > 0)
            {
                result.Status = true;
                return result;
            }
            result.Message = "At least one of the parameters in query body should have a non-null/non-empty value.";
            result.Status = false;
            return result;
        }

        public static IEnumerable<DiagnosticApiResponse> GetChildrenOfAnalysis(string analysisId, IEnumerable<DiagnosticApiResponse> allDetectors)
        {
            return allDetectors.Where(detector => detector.Metadata.AnalysisTypes != null && detector.Metadata.AnalysisTypes.Contains(analysisId));
        }

        public static Link GetDetectorLink(DiagnosticApiResponse detector, string resourceUri, string startTime, string endTime)
        {
            var baseUrl = resourceUri;
            return new Link()
            {
                Uri = $"{baseUrl}/{(detector.Metadata.Type == DetectorType.Analysis ? "analysis" : "detectors")}/{detector.Metadata.Id}?startTime={startTime}&endTime={endTime}",
                Text = $"{detector.Metadata.Name}"
            };
        }
    }
}
