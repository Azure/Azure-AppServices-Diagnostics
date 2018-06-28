using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.GeoMaster;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Utilities
{
    internal static class AzureSupportCenterInsightUtilites
    {
        private static readonly string DefaultDescription = "This insight was raised as part of the {0} detector.";
        private static readonly HashSet<string> ReservedFieldNames = new HashSet<string> { "description", "recommended action", "customer ready content" };

        private static readonly Text DefaultRecommendedAction = new Text()
        {
            IsMarkdown = false,
            Value = "Go to applens to see more information about this insight."
        };

        internal static AzureSupportCenterInsight CreateInsight<TResource>(Insight insight, OperationContext<TResource> context, Definition detector)
            where TResource : IResource
        {

            // Logic in line before would have to be updated if we added more resource types. 
            // TODO: May make sense to have resource type enums contain the resource type string
            var resourceTypeString = context.Resource.ResourceType == ResourceType.App ? "sites" : "hostingEnvironments";
            var issueCategory = context.Resource.ResourceType == ResourceType.App ? "Web App" : "App Service Environment";
            var applensPath = $"subscriptions/{context.Resource.SubscriptionId}/resourceGroups/{context.Resource.ResourceGroup}/{resourceTypeString}/{context.Resource.Name}/detectors/{detector.Id}";

            var description = GetTextObjectFromData("description", insight.Body) ?? new Text() { IsMarkdown = false, Value = string.Format(DefaultDescription, detector.Name) };
            var recommendedAction = GetTextObjectFromData("recommended action", insight.Body) ?? DefaultRecommendedAction;
            var customerReadyContent = GetTextObjectFromData("customer ready content", insight.Body);

            var supportCenterInsight = new AzureSupportCenterInsight()
            {
                Id = GetDetectorGuid(detector.Id),
                Title = insight.Message,
                ImportanceLevel = (ImportanceLevel)Enum.Parse(typeof(ImportanceLevel), ((int)insight.Status).ToString()),
                InsightFriendlyName = insight.Message,
                IssueCategory = issueCategory.ToUpper(),
                IssueSubCategory = issueCategory,
                Description = description,
                RecommendedAction = new RecommendedAction()
                {
                    Id = Guid.NewGuid(),
                    Text = recommendedAction
                },
                CustomerReadyContent = customerReadyContent == null ? null :
                    new CustomerReadyContent()
                    {
                        Text = customerReadyContent
                    },
                AdditionalDetails = GetExtraNameValuePairs(insight.Body),
                ConfidenceLevel = InsightConfidenceLevel.High,
                Scope = InsightScope.ResourceLevel,
                Links = new List<Link>()
                {
                    new Link()
                        {
                            Type = 2,
                            Text = "Applens Link",
                            Uri = $"https://applens.azurewebsites.net/{applensPath}"
                        }
                }
            };

            return supportCenterInsight;
        }

        private static Guid GetDetectorGuid(string detector)
        {

            Encoding utf8 = Encoding.UTF8;
            int count = utf8.GetByteCount(detector);
            byte[] bytes = new byte[count > 16 ? count : 16];
            Encoding.UTF8.GetBytes(detector, 0, detector.Length, bytes, 0);
            // Guid only takes byte array of length 16
            return new Guid(bytes.Take(16).ToArray());
        }

        private static IEnumerable<KeyValuePair<string, string>> GetExtraNameValuePairs(Dictionary<string, string> data)
        {
            if (data == null)
            {
                return null;
            }

            return data.Where(k => ReservedFieldNames.Contains(k.Key.ToLower()) && !k.Value.Contains("<markdown>"));
        }

        // Returns true if markdown
        private static Text GetTextObjectFromData(string searchKey, Dictionary<string, string> data)
        {
            if (data == null)
            {
                return null;
            }

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
