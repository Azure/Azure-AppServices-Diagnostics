using Diagnostics.DataProviders;
using Diagnostics.Reporting.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Diagnostics.Reporting
{
    public abstract class ProductLevelMetricsBase
    {
        protected abstract string GetProductIds();

        protected abstract bool ShowSubCategoryLevelData();

        protected abstract string GetCategoriesToShowSubCategoryLevelData();

        private string overallProductQuery = @"cluster('Usage360').database('Product360'). 
            SupportProductionDeflectionWeeklyVer1020
            | extend period = Timestamp
            | where period >= ago(14d)
            | where(DerivedProductIDStr in ({ProductIds}))
            | where DenominatorQuantity != 0 
            | summarize deflection = round(100 * sum(NumeratorQuantity) / sum(DenominatorQuantity), 2), casesLeaked = round(sum(DenominatorQuantity)) - round(sum(NumeratorQuantity)), casesDeflected = round(sum(NumeratorQuantity))  by period, ProductName, DerivedProductIDStr
            | order by DerivedProductIDStr asc";

        private string categoryBreakdownQuery = @"cluster('Usage360').database('Product360'). 
            SupportProductionDeflectionWeeklyVer1020
            | extend period = Timestamp
            | where period >= ago(14d)
            | where (DerivedProductIDStr in ({ProductIds}))
            | where DenominatorQuantity != 0 
            | summarize deflection = round(100 * sum(NumeratorQuantity) / sum(DenominatorQuantity), 2), casesLeaked = round(sum(DenominatorQuantity)) - round(sum(NumeratorQuantity)), casesDeflected = round(sum(NumeratorQuantity))  by period, SupportTopicL2
            | where SupportTopicL2 != ''
            | order by period desc";

        public string subCategoryBreakdownQuery = @"cluster('Usage360').database('Product360'). 
            SupportProductionDeflectionWeeklyVer1020
            | extend period = Timestamp
            | where period >= ago(14d)
            | where (DerivedProductIDStr in ({ProductIds})) 
            | where SupportTopicL2 in ({Categories})
            | summarize deflection = round(100 * sum(NumeratorQuantity) / sum(DenominatorQuantity), 2), casesLeaked = round(sum(DenominatorQuantity)) - round(sum(NumeratorQuantity)), casesDeflected = round(sum(NumeratorQuantity))  by period, SupportTopicL2, SupportTopicL3, SupportTopicId
            | where SupportTopicL3 != ''
            | order by period desc
        ";

        protected void Run(KustoClient kustoClient, IConfiguration config, string productName, string emailSubject, List<string> toList)
        {
            string productIds = string.Join(',', GetProductIds());
            List<Product> productList = Helper.GetOverallProductsData(kustoClient, overallProductQuery.Replace("{ProductIds}", productIds));
            if (productList == null || !productList.Any())
            {
                return;
            }

            DateTime period = productList.OrderByDescending(g => g.Period).First().Period;

            emailSubject = emailSubject.Replace("{date}", $"{period.Month}/{period.Day}");
            string emailtemplate = File.ReadAllText(@"EmailTemplates\ProductLevelMetricsTemplate.html");
            string htmlEmail = emailtemplate
                .Replace("{WeekDate}", $"{period.Month}/{period.Day}")
                .Replace("{ProductName}", productName);
            
            List<Category> categoryList = Helper.GetCategoryBreakdownData(kustoClient, categoryBreakdownQuery.Replace("{ProductIds}", productIds));
            if(categoryList == null || !categoryList.Any())
            {
                return;
            }

            htmlEmail = htmlEmail
                .Replace("{ProductMetricsTable}", Helper.GetProductMetricsTable(productList))
                .Replace("{CategoryMetricsTable}", Helper.GetCategoryBreakdownMetricsTable(categoryList));

            string subCategoryMetricsTable = string.Empty;

            if (ShowSubCategoryLevelData())
            {
                List<SubCategory> subCategoryList = Helper.GetSubCategoryBreakdownData(kustoClient, subCategoryBreakdownQuery.Replace("{ProductIds}", productIds).Replace("{Categories}", GetCategoriesToShowSubCategoryLevelData()));
                if(subCategoryList != null && subCategoryList.Any())
                {
                    subCategoryMetricsTable = Helper.GetSubCategoryBreakdownMetricsTable(subCategoryList);
                }
            }

            htmlEmail = htmlEmail.Replace("{SubCategoryMetricsTable}", subCategoryMetricsTable);
            var res = EmailClient.SendEmail(config, toList, emailSubject, "", htmlEmail).Result;
        }
    }
}
