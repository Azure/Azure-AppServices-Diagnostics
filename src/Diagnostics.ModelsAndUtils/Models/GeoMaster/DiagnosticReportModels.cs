using System;
using System.Collections.Generic;
using System.Data;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class DiagnosticReportEnvelope
    {
        public int TotalInsightsFound { get; set; }

        public string ErrorMessage { get; set; }

        public string CorrelationId { get; set; }

        public IEnumerable<DiagnosticReportInsight> Insights { get; set; }
    }

    public class DiagnosticReportInsight
    {
        public InsightStatus Status { get; set; }
        public string DetectorId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public Link DetailsLink { get; set; }

        public List<Solution> Solutions { get; set; }

        public DataTable Table { get; set; }

        public IEnumerable<KeyValuePair<string, string>> AdditionalDetails { get; set; }
    }
}
