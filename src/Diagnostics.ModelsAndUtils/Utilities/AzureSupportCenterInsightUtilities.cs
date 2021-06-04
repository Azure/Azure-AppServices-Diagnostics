using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;

namespace Diagnostics.ModelsAndUtils.Utilities
{
    public static class AzureSupportCenterInsightUtilites
    {
        private static readonly string DefaultDescription = "This insight was raised as part of the {0} detector.";
        public static readonly string DefaultInsightGuid = "9a666d9e-23b4-4502-95b7-1a00a0419ce4";
        public static readonly string SiteNotFoundInsightGuid = "0a35e298-7676-40aa-aa74-00cdc8dc8576";

        private static readonly Text DefaultRecommendedAction = new Text("Go to applens to see more information about this insight.");

        public static AzureSupportCenterInsight CreateInsight<TResource>(Insight insight, OperationContext<TResource> context, Definition detector)
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
            var category = detector.Name;
            var applensPath = $"subscriptions/{context.Resource.SubscriptionId}/resourceGroups/{context.Resource.ResourceGroup}/providers/{context.Resource.Provider}/{context.Resource.ResourceTypeName}/{context.Resource.Name}/detectors/{detector.Id}?startTime={context.StartTime}&endTime={context.EndTime}";

            string customerReadyContentText = customerReadyContent?.Value ?? "Please submit a feedback for the detector author via AppLens to populate customer ready content.";
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
                InsightFriendlyName = category,
                IssueCategory = category,
                IssueSubCategory = category,
                Description = description?? new Text("Please submit a feedback for the detector author via AppLens to supply a description for this insight.", false),
                RecommendedAction = new RecommendedAction()
                {
                    Id = Guid.NewGuid(),
                    Text = recommendedAction?? new Text("Please submit a feedback for the detector author via AppLens to populate recommended action.", false)
                },
                CustomerReadyContent = new CustomerReadyContent()
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
                            Uri = $"https://applens-preview.azurewebsites.net/{applensPath}"
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
            foreach (var detector in detectorsRun)
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
                            Uri = $"https://applens-preview.azurewebsites.net/{applensPath}"
                        }
                }
            };

            return supportCenterInsight;
        }

        public static AzureSupportCenterInsight CreateErrorMessageInsight(string errMsg, string recommendedAction)
        {
            var supportCenterInsight = new AzureSupportCenterInsight()
            {
                Id = Guid.NewGuid(),
                Title = "Error occurred while generating insight.",
                ImportanceLevel = ImportanceLevel.Info,
                InsightFriendlyName = "Error message insight.",
                IssueCategory = "ERROR MESSAGE INSIGHT",
                IssueSubCategory = "errormessageinsight",                
                Description = new Text($"**Error:** {errMsg}", true),
                RecommendedAction = new RecommendedAction()
                {
                    Id = Guid.NewGuid(),
                    Text = new Text(recommendedAction, true)
                },
                ConfidenceLevel = InsightConfidenceLevel.High,
                Scope = InsightScope.ResourceLevel
            };        
            return supportCenterInsight;
         }

        /// <summary>
        /// Use this when the site is not found from observer
        /// </summary>
        public static AzureSupportCenterInsight CreateSiteNotFoundInsight(string subscriptionId, string resourceGroupName, string siteName)
        {
            string kustoWebLink = $"https://dataexplorer.azure.com/clusters/wawswus.kusto.windows.net/databases/wawsprod?" +
                $"query=All%28%27AntaresAdminSubscriptionAuditEvents%27%29%0D%0A++++%7C+where+PreciseTimeStamp+%3E%3D+ago" +
                $"%2830d%29%0D%0A++++%7C+where+SubscriptionId+%3D%7E+%27{subscriptionId}%27+and+ResourceGroupName+%3D%7E+" +
                $"%27{resourceGroupName}%27+and+SiteName+%3D%7E+%27{siteName}%27%0D%0A++++%7C+where+OperationType+%3D%7E%" +
                $"27Delete%27+and+EntityType+%3D%7E%27WebSite%27+and+OperationStatus+%3D%7E+%27Success%27%0D%0A++++%7C+wh" +
                $"ere+ResourceGroupName+%21%3D+%27%27+and++ResourceGroupName+%21%3D+%27None%27+%0D%0A++++%7C+order+by+Pre" +
                $"ciseTimeStamp+asc%0D%0A++++%7C+project+PreciseTimeStamp%2C+SubscriptionId+%2C+ResourceGroupName+%2C+Sit" +
                $"eName+%2C+StampName%2C+EventStampName%2C+Address%0D%0A++++%7C+summarize+Timestamps+%3D+make_set%28bin%2" +
                $"8PreciseTimeStamp%2C+1d%29%29+by+SubscriptionId+%2C+ResourceGroupName+%2C+SiteName+%2C+StampName%2C+Eve" +
                $"ntStampName%2C+Address%0D%0A++++%7C+project-reorder+Timestamps";

            var supportCenterInsight = new AzureSupportCenterInsight()
            {
                Id = Guid.Parse(SiteNotFoundInsightGuid),
                Title = $"{siteName} could not be found in resource group {resourceGroupName} in subscription {subscriptionId}",
                ImportanceLevel = ImportanceLevel.Critical,
                InsightFriendlyName = "Site not found",
                IssueCategory = "SITE NOT FOUND",
                IssueSubCategory = "sitenotfound",
                Description = new Text($"Failed to find **{siteName}** in subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName} in GeoMaster databases", true),
                RecommendedAction = new RecommendedAction()
                {
                    Id = Guid.NewGuid(),
                    Text = new Text("Check in Resource Explorer if the site exists or check the resource deletion records in Kusto to see if it was already deleted.")
                },
                ConfidenceLevel = InsightConfidenceLevel.High,
                Scope = InsightScope.ResourceLevel,
                Links = new List<Link>()
                {
                    new Link()
                        {
                            Type = 0,
                            Text = "Kusto Query",
                            Uri = kustoWebLink
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
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var isMarkdown = input.Contains("<markdown>");

            var output = new Text(isMarkdown ? input.Replace("<markdown>", "").Replace("</markdown>", "") : input, isMarkdown);

            return output;
        }
    }
}
