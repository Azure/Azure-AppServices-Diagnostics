using Diagnostics.DataProviders;
using Diagnostics.Reporting.Models;
using Microsoft.Extensions.Configuration;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Diagnostics.Reporting
{
    public static class CategoryLevelMetrics
    {
        private static string _productBreakdownQuery = $@"cluster('Usage360').database('Product360'). 
            {P360TableResolver.SupportProductionDeflectionWeeklyTable}
            | extend period = Timestamp
            | where period >= ago(14d)
            | where (DerivedProductIDStr in ({{ProductIds}})) 
            | where SupportTopicL2 contains '{{ProglemCategory}}'
            | where DenominatorQuantity != 0 
            | summarize deflection = round(100 * sum(NumeratorQuantity) / sum(DenominatorQuantity), 2), casesLeaked = round(sum(DenominatorQuantity)) - round(sum(NumeratorQuantity)), casesDeflected = round(sum(NumeratorQuantity))  by period, ProductName, DerivedProductIDStr
            | order by DerivedProductIDStr asc";

        private static string _productWeeklyTrendsQuery = $@"
            {P360TableResolver.SupportProductionDeflectionWeeklyTable}
                | extend period = Timestamp
                | where period >= ago(150d)
                | where (DerivedProductIDStr in ({{ProductIds}}))
                | where SupportTopicL2 contains '{{ProglemCategory}}'
                | where DenominatorQuantity != 0 
                | summarize qty = sum(NumeratorQuantity) / sum(DenominatorQuantity), auxQty = sum(DenominatorQuantity) by period, ProductName
                | project period , ProductName , deflection = round(100 * qty, 2)";

        private static string _subCategoryBreakdownQuery = $@"cluster('Usage360').database('Product360'). 
            {P360TableResolver.SupportProductionDeflectionWeeklyTable}
            | extend period = Timestamp
            | where period >= ago(14d)
            | where (DerivedProductIDStr in ({{ProductIds}})) 
            | where SupportTopicL2 contains '{{ProglemCategory}}'
            | summarize deflection = round(100 * sum(NumeratorQuantity) / sum(DenominatorQuantity), 2), casesLeaked = round(sum(DenominatorQuantity)) - round(sum(NumeratorQuantity)), casesDeflected = round(sum(NumeratorQuantity))  by period, SupportTopicL2, SupportTopicL3, SupportTopicId
            | where SupportTopicL3 != ''
            | order by period desc";

        private static string _casesLeakedQuery = @"
            let endTime = datetime({endTime});
            let startTime = datetime({startTime});
            cluster('Usage360').database('Product360').
            AllCloudSupportIncidentDataWithP360MetadataMapping
            | where DerivedProductIDStr in ({ProductIds})
            | where Incidents_SupportTopicL2Current contains '{ProglemCategory}'
            | where Incidents_CreatedTime > startTime and Incidents_CreatedTime <= endTime
            | summarize IncidentTime = any(Incidents_CreatedTime) by Incidents_IncidentId , Incidents_Severity , Incidents_ProductName , Incidents_SupportTopicL2Current , Incidents_SupportTopicL3Current 
            | extend SupportCenterCaseLink = strcat(""https://azuresupportcenter.msftcloudes.com/caseoverview?srId="", Incidents_IncidentId)
            | order by Incidents_SupportTopicL3Current asc";

        private static string breakdownBySolutionQuery = $@"
            let processedStream = cluster('Usage360').database('Product360').
                {P360TableResolver.SupportProductionDeflectionWeeklyPoPInsightsTable}
                | extend Current = CurrentDenominatorQuantity, Previous = PreviousDenominatorQuantity, PreviousN = PreviousNDenominatorQuantity , CurrentQ = CurrentNumeratorQuantity, PreviousQ = PreviousNumeratorQuantity, PreviousNQ = PreviousNNumeratorQuantity,  Change = CurrentNumeratorQuantity-PreviousNumeratorQuantity
                | extend C_ID = SolutionType, C_Name = SolutionType
                | where DerivedProductIDStr in ({{ProductIds}})
                | where C_ID != """"
                | summarize C_Name=any(C_Name), Current= sum(Current), Previous = sum(Previous), PreviousN = sum(PreviousN), CurrentQ = sum(CurrentNumeratorQuantity), PreviousQ = sum(PreviousNumeratorQuantity), PreviousNQ = sum(PreviousNNumeratorQuantity) by C_ID, ProductName, SupportTopicL2 | extend Change = Current - Previous
                | extend CurPer = iff(Current == 0, todouble(''), CurrentQ/Current), PrevPer = iff(Previous == 0, todouble(''), PreviousQ/Previous), NPrevPer = iff(PreviousN == 0, todouble(''), PreviousNQ/PreviousN);
            processedStream
                | order by Current desc, Previous desc, PreviousN desc
                | limit 100
                | project ProductName, SupportTopicL2, C_ID, C_Name = iif(isempty(C_Name),C_ID,C_Name), Current, CurPer, Previous, PrevPer, Change, PreviousN, NPrevPer
                | order by Current desc, Previous desc, PreviousN desc 
                | project ProductName, SupportTopicL2, Name = C_Name, Current = round(100 * CurPer, 2), CurrentNumerator = round(CurPer * Current), CurrentDenominator = round(Current), Previous = round(100 * PrevPer, 2), PreviousNumerator = round(PrevPer * Previous), PreviousDenominator = round(Previous)
                | order by ProductName desc
                | where SupportTopicL2 contains '{{ProglemCategory}}'";

        public static void Run(KustoClient kustoClient, IConfiguration config)
        {
            var configurations = config.GetSection("CategoryLevel");
            var entries = configurations.GetChildren();
            foreach(var entry in entries)
            {
                CreateAndSendReport(config, entry, kustoClient);
            }
        }

        private static void CreateAndSendReport(IConfiguration config, IConfigurationSection entry, KustoClient kustoClient)
        {
            string categoryName = config[$"{entry.Path}:Name"];
            string productIds = config[$"{entry.Path}:ProductIds"];
            string emailSubject = config[$"{entry.Path}:Subject"];
            List<string> toList = config[$"{entry.Path}:To"].Split(new char[] { ',', ';' }).ToList();

            List<Product> productList = DataHelper.GetOverallProductsData(kustoClient, _productBreakdownQuery.Replace("{ProductIds}", productIds).Replace("{ProglemCategory}", categoryName));
            List<Tuple<string, Image>> productWeeklyTrends = DataHelper.GetProductWeeklyTrends(kustoClient, _productWeeklyTrendsQuery.Replace("{ProductIds}", productIds).Replace("{ProglemCategory}", categoryName));
            string subCategoryBreakdownQuery = _subCategoryBreakdownQuery.Replace("{ProductIds}", productIds).Replace("{ProglemCategory}", categoryName);
            List<SubCategory> subCategoryList = DataHelper.GetSubCategoryBreakdownData(kustoClient, subCategoryBreakdownQuery);
            List<Solution> solutions = DataHelper.GetSolutionsData(kustoClient, breakdownBySolutionQuery.Replace("{ProductIds}", productIds).Replace("{ProglemCategory}", categoryName));

            DateTime period = productList.OrderByDescending(g => g.Period).First().Period;
            emailSubject = emailSubject.Replace("{date}", $"{period.Month}/{period.Day}");

            SendGridMessage sendGridMessage = EmailClient.InitializeMessage(config, emailSubject, toList);

            string emailtemplate = File.ReadAllText(@"EmailTemplates\CategoryLevelMetricsTemplate.html");
            string htmlEmail = emailtemplate.Replace("{WeekDate}", $"{period.Month}/{period.Day}").Replace("{CategoryName}", categoryName);

            htmlEmail = htmlEmail.Replace("{ProductMetricsTable}", HtmlHelper.GetProductMetricsTable(productList, productWeeklyTrends, ref sendGridMessage));

            string casesLeakedQuery = _casesLeakedQuery
                .Replace("{ProductIds}", productIds)
                .Replace("{ProglemCategory}", categoryName)
                .Replace("{endTime}", $"{period.Year}-{period.Month}-{period.Day}")
                .Replace("{startTime}", $"{period.AddDays(-7).Year}-{period.AddDays(-7).Month}-{period.AddDays(-7).Day}");

            htmlEmail = htmlEmail
                .Replace("{LeakedCasesLink}", kustoClient.GetKustoQueryUriAsync("*", casesLeakedQuery).Result)
                .Replace("{SubCategoryBreakdownLink}", kustoClient.GetKustoQueryUriAsync("*", subCategoryBreakdownQuery).Result)
                .Replace("{SolutionMetricsTable}", HtmlHelper.GetSolutionMetricsTable(solutions));

            string subCategoryMetricsTable = HtmlHelper.GetSubCategoryBreakdownMetricsTable(subCategoryList);
            htmlEmail = htmlEmail.Replace("{SubCategoryMetricsTable}", subCategoryMetricsTable);

            var res = EmailClient.SendEmail(config, sendGridMessage, htmlEmail).Result;
        }
    }
}
