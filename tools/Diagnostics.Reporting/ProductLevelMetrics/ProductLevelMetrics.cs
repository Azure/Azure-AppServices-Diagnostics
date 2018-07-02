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
    public static class ProductLevelMetrics
    {
        private static string _overallProductQuery = $@"cluster('Usage360').database('Product360'). 
            {P360TableResolver.SupportProductionDeflectionWeeklyTable}
            | extend period = Timestamp
            | where period >= ago(14d)
            | where(DerivedProductIDStr in ({{ProductIds}}))
            | where DenominatorQuantity != 0 
            | summarize deflection = round(100 * sum(NumeratorQuantity) / sum(DenominatorQuantity), 2), casesLeaked = round(sum(DenominatorQuantity)) - round(sum(NumeratorQuantity)), casesDeflected = round(sum(NumeratorQuantity))  by period, ProductName, DerivedProductIDStr
            | order by DerivedProductIDStr asc";

        private static string _productWeeklyTrendsQuery = $@"
            {P360TableResolver.SupportProductionDeflectionWeeklyTable}
                | extend period = Timestamp
                | where period >= ago(150d)
                | where (DerivedProductIDStr in ({{ProductIds}}))
                | where DenominatorQuantity != 0 
                | summarize qty = sum(NumeratorQuantity) / sum(DenominatorQuantity), auxQty = sum(DenominatorQuantity) by period, ProductName
                | project period , ProductName , deflection = round(100 * qty, 2)";

        private static string _categoryBreakdownQuery = @"cluster('Usage360').database('Product360'). 
            SupportProductionDeflectionWeeklyVer1021
            | extend period = Timestamp
            | where period >= ago(14d)
            | where (DerivedProductIDStr in ({ProductIds}))
            | where DenominatorQuantity != 0 
            | summarize deflection = round(100 * sum(NumeratorQuantity) / sum(DenominatorQuantity), 2), casesLeaked = round(sum(DenominatorQuantity)) - round(sum(NumeratorQuantity)), casesDeflected = round(sum(NumeratorQuantity))  by period, SupportTopicL2
            | where SupportTopicL2 != ''
            | order by period desc";

        private static string _subCategoryBreakdownQuery = $@"cluster('Usage360').database('Product360'). 
            {P360TableResolver.SupportProductionDeflectionWeeklyTable}
            | extend period = Timestamp
            | where period >= ago(14d)
            | where (DerivedProductIDStr in ({{ProductIds}})) 
            | where SupportTopicL2 in ({{Categories}})
            | summarize deflection = round(100 * sum(NumeratorQuantity) / sum(DenominatorQuantity), 2), casesLeaked = round(sum(DenominatorQuantity)) - round(sum(NumeratorQuantity)), casesDeflected = round(sum(NumeratorQuantity))  by period, SupportTopicL2, SupportTopicL3, SupportTopicId
            | where SupportTopicL3 != ''
            | order by period desc
        ";

        private static string _casesLeakedQuery = @"cluster('Usage360').database('Product360').
            AllCloudSupportIncidentDataWithP360MetadataMapping
            | where DerivedProductIDStr in ({ProductIds})
            | where Incidents_CreatedTime > ago(7d)
            | summarize by Incidents_IncidentId , Incidents_Severity , Incidents_ProductName , Incidents_SupportTopicL2Current , Incidents_SupportTopicL3Current 
            | order by Incidents_SupportTopicL2Current asc 
            ";

        private static string breakdownBySolutionQuery = $@"
            let processedStream = cluster('Usage360').database('Product360').
                {P360TableResolver.SupportProductionDeflectionWeeklyPoPInsightsTable}
                | extend Current = CurrentDenominatorQuantity, Previous = PreviousDenominatorQuantity, PreviousN = PreviousNDenominatorQuantity , CurrentQ = CurrentNumeratorQuantity, PreviousQ = PreviousNumeratorQuantity, PreviousNQ = PreviousNNumeratorQuantity,  Change = CurrentNumeratorQuantity-PreviousNumeratorQuantity
                | extend C_ID = SolutionType, C_Name = SolutionType
                | where (DerivedProductIDStr in ({{ProductIds}}))
                | where C_ID != """"
                | summarize C_Name=any(C_Name), Current= sum(Current), Previous = sum(Previous), PreviousN = sum(PreviousN), CurrentQ = sum(CurrentNumeratorQuantity), PreviousQ = sum(PreviousNumeratorQuantity), PreviousNQ = sum(PreviousNNumeratorQuantity) by C_ID, ProductName | extend Change = Current - Previous
                | extend CurPer = iff(Current == 0, todouble(''), CurrentQ/Current), PrevPer = iff(Previous == 0, todouble(''), PreviousQ/Previous), NPrevPer = iff(PreviousN == 0, todouble(''), PreviousNQ/PreviousN);
            processedStream
                | order by Current desc, Previous desc, PreviousN desc
                | limit 100
                | project ProductName , C_ID, C_Name = iif(isempty(C_Name),C_ID,C_Name), Current, CurPer, Previous, PrevPer, Change, PreviousN, NPrevPer
                | order by Current desc, Previous desc, PreviousN desc 
                | project ProductName, Name = C_Name, Current = round(100 * CurPer, 2), CurrentNumerator = round(CurPer * Current), CurrentDenominator = round(Current), Previous = round(100 * PrevPer, 2), PreviousNumerator = round(PrevPer * Previous), PreviousDenominator = round(Previous)
                | order by ProductName desc";

        public static void Run(KustoClient kustoClient, IConfiguration config)
        {
            var configurations = config.GetSection("ProductLevel");
            var entries = configurations.GetChildren();
            foreach (var entry in entries)
            {
                CreateAndSendReport(config, entry, kustoClient);
            }
        }

        private static void CreateAndSendReport(IConfiguration config, IConfigurationSection entry, KustoClient kustoClient)
        {
            string productName = config[$"{entry.Path}:Name"];
            string productIds = config[$"{entry.Path}:ProductIds"];
            string emailSubject = config[$"{entry.Path}:Subject"];
            List<string> toList = config[$"{entry.Path}:To"].Split(new char[] { ',', ';' }).ToList();
            bool showSubCategoryLevelData = Convert.ToBoolean(config[$"{entry.Path}:ShowSubCategoryLevelData"]);
            string categoriesToExpand = config[$"{entry.Path}:CategoriesToExpand"];

            List<Product> productList = DataHelper.GetOverallProductsData(kustoClient, _overallProductQuery.Replace("{ProductIds}", productIds));
            if (productList == null || !productList.Any())
            {
                return;
            }

            List<Tuple<string, Image>> productWeeklyTrends = DataHelper.GetProductWeeklyTrends(kustoClient, _productWeeklyTrendsQuery.Replace("{ProductIds}", productIds));
            List<Category> categoryList = DataHelper.GetCategoryBreakdownData(kustoClient, _categoryBreakdownQuery.Replace("{ProductIds}", productIds));
            List<Solution> solutions = DataHelper.GetSolutionsData(kustoClient, breakdownBySolutionQuery.Replace("{ProductIds}", productIds));

            DateTime period = productList.OrderByDescending(g => g.Period).First().Period;
            emailSubject = emailSubject.Replace("{date}", $"{period.Month}/{period.Day}");

            SendGridMessage sendGridMessage = EmailClient.InitializeMessage(config, emailSubject, toList);

            string emailtemplate = File.ReadAllText(@"EmailTemplates\ProductLevelMetricsTemplate.html");
            string htmlEmail = emailtemplate
                .Replace("{WeekDate}", $"{period.Month}/{period.Day}")
                .Replace("{ProductName}", productName)
                .Replace("{LeakedCasesLink}", kustoClient.GetKustoQueryUriAsync("*", _casesLeakedQuery.Replace("{ProductIds}", productIds)).Result)
                .Replace("{CategoryBreakdownLink}", kustoClient.GetKustoQueryUriAsync("*", _categoryBreakdownQuery.Replace("{ProductIds}", productIds)).Result);
            
            htmlEmail = htmlEmail
                .Replace("{ProductMetricsTable}", HtmlHelper.GetProductMetricsTable(productList, productWeeklyTrends, ref sendGridMessage))
                .Replace("{CategoryMetricsTable}", HtmlHelper.GetCategoryBreakdownMetricsTable(categoryList))
                .Replace("{SolutionMetricsTable}", HtmlHelper.GetSolutionMetricsTable(solutions));

            string subCategoryMetricsTable = string.Empty;

            if (showSubCategoryLevelData)
            {
                List<SubCategory> subCategoryList = DataHelper.GetSubCategoryBreakdownData(kustoClient, _subCategoryBreakdownQuery.Replace("{ProductIds}", productIds).Replace("{Categories}", categoriesToExpand));
                if (subCategoryList != null && subCategoryList.Any())
                {
                    subCategoryMetricsTable = HtmlHelper.GetSubCategoryBreakdownMetricsTable(subCategoryList);
                }
            }

            htmlEmail = htmlEmail.Replace("{SubCategoryMetricsTable}", subCategoryMetricsTable);
            var res = EmailClient.SendEmail(config, sendGridMessage, htmlEmail).Result;
        }
    }
}
