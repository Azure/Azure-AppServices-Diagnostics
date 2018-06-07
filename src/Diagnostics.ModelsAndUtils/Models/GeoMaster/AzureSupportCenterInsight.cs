using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.GeoMaster
{
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

        public string ConfidenceLevel { get; set; }

        public string Scope { get; set; }
    }

    public enum ImportanceLevel
    {
        SomeLevel
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

    public static class AzureSupportCenterInsightUtilites
    {
        public static AzureSupportCenterInsight CreateInsight<TResource>(Insight insight, OperationContext<TResource> context, Definition detector)
            where TResource : IResource
        {

            // Logic in line before would have to be updated if we added more resource types. 
            // TODO: May make sense to have resource type enums contain the resource type string
            var resourceTypeString = context.Resource.ResourceType == ResourceType.App ? "sites" : "hostingEnvironments";
            var applensPath = $"subscriptions/{context.Resource.SubscriptionId}/resourceGroups/{context.Resource.ResourceGroup}/{resourceTypeString}/{context.Resource.Name}/detectors/{detector.Id}";

            var recommendedAction = GetTextObjectFromData("recommended action", insight.Body);
            var description = GetTextObjectFromData("description", insight.Body);
            var customerReadyContent = GetTextObjectFromData("customer ready content", insight.Body);

            var supportCenterInsight = new AzureSupportCenterInsight()
            {
                Id = GetDetectorGuid(detector.Id),
                Title = insight.Message,
                ImportanceLevel = ImportanceLevel.SomeLevel,
                InsightFriendlyName = insight.Message,
                IssueCategory = detector.Name.ToUpper(),
                IssueSubCategory = detector.Name,
                Description = description,
                Links = new List<Link>()
                    {
                        new Link()
                        {
                            Type = 0,
                            Text = "Applens Link",
                            Uri = $"https://applens.azurewebsites.net/{applensPath}"
                        }
                    },
                RecommendedAction = recommendedAction == null ? null : 
                    new RecommendedAction()
                    {
                        Id = new Guid(),
                        Text = recommendedAction
                    },
                CustomerReadyContent = customerReadyContent == null ? null :
                    new CustomerReadyContent()
                    {
                        Text = customerReadyContent
                    },
                ConfidenceLevel = "High",
                Scope = "ResourceLevel"
            };

            return supportCenterInsight;
        }

        private static Guid GetDetectorGuid(string detector)
        {
            try
            {
                Encoding utf8 = Encoding.UTF8;
                int count = utf8.GetByteCount(detector);
                byte[] bytes = new byte[count > 16 ? count : 16];
                Encoding.UTF8.GetBytes(detector, 0, detector.Length, bytes, 0);
                // Guid only takes byte array of length 16
                return new Guid(bytes.Take(16).ToArray());
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return new Guid();
        }

        // Returns true if markdown
        private static Text GetTextObjectFromData(string searchKey, Dictionary<string, string> data)
        {
            var matchingNameValuePairs = data.Where(x => x.Key.ToLower() == searchKey);
            var matchingString = matchingNameValuePairs.Any() ? matchingNameValuePairs.FirstOrDefault().Value : null;

            if (string.IsNullOrWhiteSpace(matchingString))
            {
                return null;
            }

            var isMarkdown = matchingString.Contains("<markdown>");

            var output = new Text()
            {
                IsMarkdown = isMarkdown,
                Value = isMarkdown ? matchingString.Replace("<markdown>", "").Replace("</markdown>", "") : matchingString
            };

            return output;
        }
    }
}
