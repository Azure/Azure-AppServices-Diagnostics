using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.GeoMaster
{
    public class AzureSupportCenterInsightEnvelope
    {
        public int TotalInsightsFound { get; set; }

        public string ErrorMessage { get; set; }

        public Guid CorrelationId { get; set; }

        public string Metadata1 { get; set; }

        public IEnumerable<AzureSupportCenterInsight> Insights { get; set; }
    }

    public class AzureSupportCenterInsight
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public ImportanceLevel ImportanceLevel { get; set; }

        public string InsightFriendlyName { get; set; }

        public string IssueCategory { get; set; }

        public string IssueSubCategory { get; set; }

        public Text Description { get; set; }

        public RecommendedAction RecommendedAction { get; set; }

        public CustomerReadyContent CustomerReadyContent { get; set; }

        public IEnumerable<Link> Links { get; set; }

        public InsightConfidenceLevel ConfidenceLevel { get; set; }

        public InsightScope Scope { get; set; }

        public IEnumerable<Link> ApplensLinks { get; set; }

        public IEnumerable<KeyValuePair<string, string>> AdditionalDetails { get; set; }
    }

    public class CustomerReadyContent
    {
        public string ArticleId { get; set; }

        public Text Text { get; set; }
    }

    public class Text
    {
        public bool IsMarkdown { get; set; }

        public string Value { get; set; }
    }

    public class Link
    {
        public int Type { get; set; }

        public string Text { get; set; }

        public string Uri { get; set; }
    }

    public class RecommendedAction
    {
        public Guid Id { get; set; }

        public Text Text { get; set; }
    }

    public enum ImportanceLevel
    {
        Critical = 0,

        Warning = 1,

        Info = 2,
    }

    public enum InsightScope
    {
        ResourceLevel = 0,

        SubscriptionLevel = 1,
    }



    public enum InsightConfidenceLevel
    {
        VeryLow = 0,

        Low = 1,

        Medium = 2,

        High = 3,

        VeryHigh = 4,
    }
}
