using Diagnostics.DataProviders;
using Diagnostics.Reporting.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Diagnostics.Reporting
{
    public static class OverallMetrics
    {
        private static string query = @"cluster('Usage360').database('Product360'). 
            SupportProductionDeflectionWeeklyVer1020
            | extend period = Timestamp
            | where period >= ago(14d)
            | where(DerivedProductIDStr in ('14748', '16170', '16333', '16072', '16512', '16513'))
            | where DenominatorQuantity != 0 
            | summarize deflection = round(100 * sum(NumeratorQuantity) / sum(DenominatorQuantity), 2), casesLeaked = round(sum(DenominatorQuantity)) - round(sum(NumeratorQuantity)), casesDeflected = round(sum(NumeratorQuantity))  by period, ProductName, DerivedProductIDStr
            | order by DerivedProductIDStr asc";

        private static string breakdownBySolutionQuery = @"
            let processedStream = cluster('Usage360').database('Product360').SupportProductionDeflectionWeeklyPoPInsightsVer1020
                | extend Current = CurrentDenominatorQuantity, Previous = PreviousDenominatorQuantity, PreviousN = PreviousNDenominatorQuantity , CurrentQ = CurrentNumeratorQuantity, PreviousQ = PreviousNumeratorQuantity, PreviousNQ = PreviousNNumeratorQuantity,  Change = CurrentNumeratorQuantity-PreviousNumeratorQuantity
                | extend C_ID = SolutionType, C_Name = SolutionType
                | where (DerivedProductIDStr in ('14748', '16170', '16333', '16072', '16512', '16513'))
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
            List<Product> productList = Helper.GetOverallProductsData(kustoClient, query);
            if (productList == null || !productList.Any())
            {
                return;
            }

            DateTime period = productList.OrderByDescending(g => g.Period).First().Period;

            string emailSubject = config["OverallMetricsReport:Subject"].ToString().Replace("{date}", $"{period.Month}/{period.Day}");
            List<string> toList = config["OverallMetricsReport:To"].ToString().Split(new char[] { ',', ';', ':' }).ToList();

            string emailtemplate = File.ReadAllText(@"EmailTemplates\OverallMetricsTemplate.html");
            string htmlEmail = emailtemplate.Replace("{WeekDate}", $"{period.Month}/{period.Day}");

            htmlEmail = htmlEmail.Replace("{ProductMetricsTable}", Helper.GetProductMetricsTable(productList));

            List<Solution> solutions = Helper.GetSolutionsData(kustoClient, breakdownBySolutionQuery);
            htmlEmail = htmlEmail.Replace("{SolutionMetricsTable}", Helper.GetSolutionMetricsTable(solutions));

            var res = EmailClient.SendEmail(config, toList, emailSubject, "", htmlEmail).Result;
        }
    }
}
