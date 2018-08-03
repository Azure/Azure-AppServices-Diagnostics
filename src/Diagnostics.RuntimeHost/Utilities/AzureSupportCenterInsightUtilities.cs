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
        public static readonly string DefaultInsightGuid = "9a666d9e-23b4-4502-95b7-1a00a0419ce4";

        private static readonly Text DefaultRecommendedAction = new Text()
        {
            IsMarkdown = false,
            Value = "Go to applens to see more information about this insight."
        };

        internal static AzureSupportCenterInsight CreateInsight<TResource>(Insight insight, OperationContext<TResource> context, ArmResourceType armResourceType,  Definition detector)
            where TResource : IResource
        {
            var applensPath = $"subscriptions/{context.Resource.SubscriptionId}/resourceGroups/{context.Resource.ResourceGroup}/providers/{armResourceType.Provider}/{armResourceType.ResourceTypeName}/{context.Resource.Name}/detectors/{detector.Id}?startTime={context.StartTime}&endTime={context.EndTime}";
            var category = detector.Name.Length > 32 ? armResourceType.ResourceTypeName : detector.Name;

            var description = GetTextObjectFromData("description", insight.Body) ?? new Text() { IsMarkdown = false, Value = string.Format(DefaultDescription, detector.Name) };
            var recommendedAction = GetTextObjectFromData("recommended action", insight.Body) ?? DefaultRecommendedAction;
            var customerReadyContent = GetTextObjectFromData("customer ready content", insight.Body);

            if (insight.Status == InsightStatus.Success || insight.Status == InsightStatus.None)
            {
                insight.Status = InsightStatus.Info;
            }

            var supportCenterInsight = new AzureSupportCenterInsight()
            {
                Id = GetDetectorGuid(detector.Id),
                Title = insight.Message,
                ImportanceLevel = (ImportanceLevel)Enum.Parse(typeof(ImportanceLevel), ((int)insight.Status).ToString()),
                InsightFriendlyName = insight.Message,
                IssueCategory = category,
                IssueSubCategory = category,
                Description = description,
                RecommendedAction = new RecommendedAction()
                {
                    Id = Guid.NewGuid(),
                    Text = recommendedAction
                },
                CustomerReadyContent = customerReadyContent == null ? null :
                    new CustomerReadyContent()
                    {
                        ArticleId = Guid.NewGuid(),
                        ArticleContent = customerReadyContent.Value
                    },
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

        internal static AzureSupportCenterInsight CreateDefaultInsight<TResource>(OperationContext<TResource> context, ArmResourceType resourceType, List<Definition> detectorsRun)
            where TResource : IResource
        {
            var description = new StringBuilder();
            description.AppendLine("The following detector(s) were run but no insights were found:");
            description.AppendLine();
            foreach(var detector in detectorsRun)
            {
                description.AppendLine($"* {detector.Name}");
            }
            description.AppendLine();

            var applensPath = $"subscriptions/{context.Resource.SubscriptionId}/resourceGroups/{context.Resource.ResourceGroup}/providers/{resourceType.Provider}/{resourceType.ResourceTypeName}/{context.Resource.Name}/detectors/{detectorsRun.FirstOrDefault().Id}?startTime={context.StartTime}&endTime={context.EndTime}";

            var supportCenterInsight = new AzureSupportCenterInsight()
            {
                Id = Guid.Parse(DefaultInsightGuid),
                Title = "No issues found in relevant detector(s) in applens",
                ImportanceLevel = ImportanceLevel.Info,
                InsightFriendlyName = "No Issue",
                IssueCategory = "NOISSUE",
                IssueSubCategory = "noissue",
                Description = new Text()
                {
                    IsMarkdown = true,
                    Value = description.ToString()
                },
                RecommendedAction = new RecommendedAction()
                {
                    Id = Guid.NewGuid(),
                    Text = new Text()
                    {
                        IsMarkdown = false,
                        Value = "Follow the applens link to see any other information these detectors have."
                    }
                },
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
