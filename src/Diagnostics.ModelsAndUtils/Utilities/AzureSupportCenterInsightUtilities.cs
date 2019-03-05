using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.ModelsAndUtils.Utilities
{
    public static class AzureSupportCenterInsightUtilites
    {
        private static readonly string DefaultDescription = "This insight was raised as part of the {0} detector.";
        public static readonly string DefaultInsightGuid = "9a666d9e-23b4-4502-95b7-1a00a0419ce4";

        private static readonly Text DefaultRecommendedAction = new Text("Go to applens to see more information about this insight.");

        public static AzureSupportCenterInsight CreateInsight<TResource>(Insight insight, OperationContext<TResource> context,  Definition detector)
            where TResource : IResource
        {
            var description = GetTextObjectFromData("description", insight.Body) ?? new Text(string.Format(DefaultDescription, detector.Name));
            var recommendedAction = GetTextObjectFromData("recommended action", insight.Body) ?? DefaultRecommendedAction;
            var customerReadyContent = GetTextObjectFromData("customer ready content", insight.Body);

            return CreateInsight<TResource>(insight.Message, insight.Status, description, recommendedAction, customerReadyContent, detector, context);
        }

        internal static AzureSupportCenterInsight CreateInsight<TResource>(string title, InsightStatus status, Text description, Text recommendedAction, Text customerReadyContent, Definition detector, OperationContext<TResource> context)
            where TResource : IResource
        {
            var category = detector.Name.Length > 32 ? context.Resource.ResourceTypeName : detector.Name;
            var applensPath = $"subscriptions/{context.Resource.SubscriptionId}/resourceGroups/{context.Resource.ResourceGroup}/providers/{context.Resource.Provider}/{context.Resource.ResourceTypeName}/{context.Resource.Name}/detectors/{detector.Id}?startTime={context.StartTime}&endTime={context.EndTime}";

            string customerReadyContentText = customerReadyContent?.Value;
            if (customerReadyContent != null && customerReadyContent.IsMarkdown)
            {
                // Turn the customer ready content into HTML since that is what is supported by ASC as of now
                customerReadyContentText = CommonMark.CommonMarkConverter.Convert(customerReadyContent.Value);
            }

            if (status == InsightStatus.Success || status == InsightStatus.None)
            {
                status = InsightStatus.Info;
            }

            var supportCenterInsight = new AzureSupportCenterInsight()
            {
                Id = GetDetectorGuid(title),
                Title = title,
                ImportanceLevel = (ImportanceLevel)Enum.Parse(typeof(ImportanceLevel), ((int)status).ToString()),
                InsightFriendlyName = title,
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
                        ArticleContent = customerReadyContentText
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

        public static AzureSupportCenterInsight CreateDefaultInsight<TResource>(OperationContext<TResource> context, List<Definition> detectorsRun)
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

            var applensPath = $"subscriptions/{context.Resource.SubscriptionId}/resourceGroups/{context.Resource.ResourceGroup}/providers/{context.Resource.Provider}/{context.Resource.ResourceTypeName}/{context.Resource.Name}/detectors/{detectorsRun.FirstOrDefault().Id}?startTime={context.StartTime}&endTime={context.EndTime}";

            var supportCenterInsight = new AzureSupportCenterInsight()
            {
                Id = Guid.Parse(DefaultInsightGuid),
                Title = "No issues found in relevant detector(s) in applens",
                ImportanceLevel = ImportanceLevel.Info,
                InsightFriendlyName = "No Issue",
                IssueCategory = "NOISSUE",
                IssueSubCategory = "noissue",
                Description = new Text(description.ToString(), true),
                RecommendedAction = new RecommendedAction()
                {
                    Id = Guid.NewGuid(),
                    Text = new Text("Follow the applens link to see any other information these detectors have.")
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

        private static Text GetTextObjectFromData(string searchKey, Dictionary<string, string> data)
        {
            if (data == null)
            {
                return null;
            }

            var matchingNameValuePairs = data.Where(x => x.Key.ToLower() == searchKey);
            var matchingString = matchingNameValuePairs.Any() ? matchingNameValuePairs.FirstOrDefault().Value : null;

            return string.IsNullOrWhiteSpace(matchingString) ? null : ParseForMarkdown(matchingString);
        }

        public static Text ParseForMarkdown(string input)
        {
            if(string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var isMarkdown = input.Contains("<markdown>");

            var output = new Text(isMarkdown ? input.Replace("<markdown>", "").Replace("</markdown>", "") : input, isMarkdown);

            return output;
        }
    }
}
