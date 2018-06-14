using Diagnostics.DataProviders;
using Diagnostics.Reporting.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Diagnostics.Reporting
{
    internal static class DataHelper
    {

        internal static List<Product> GetOverallProductsData(KustoClient kustoClient, string query)
        {
            var tableResult = kustoClient.ExecuteQueryAsync(query, "waws-prod-blu-000").Result;
            List<Product> products = new List<Product>();
            if (tableResult == null || tableResult.Rows == null || tableResult.Rows.Count <= 0)
            {
                return products;
            }

            foreach (DataRow row in tableResult.Rows)
            {
                Product newEntry = new Product()
                {
                    ProductId = row["DerivedProductIDStr"].ToString(),
                    ProductName = row["ProductName"].ToString(),
                    Period = DateTime.Parse(row["period"].ToString()).ToUniversalTime(),
                    CasesDeflected = Convert.ToInt32(row["casesDeflected"]),
                    CasesLeaked = Convert.ToInt32(row["casesLeaked"]),
                    DeflectionPercentage = Convert.ToDouble(row["deflection"])
                };

                products.Add(newEntry);
            }

            return products;
        }

        internal static List<Tuple<string, Image>> GetProductWeeklyTrends(KustoClient kustoClient, string query)
        {
            var tableResult = kustoClient.ExecuteQueryAsync(query, "waws-prod-blu-000").Result;
            List<Tuple<string, Image>> output = new List<Tuple<string, Image>>();

            if (tableResult == null || tableResult.Rows == null || tableResult.Rows.Count <= 0)
            {
                return output;
            }

            List<Product> products = new List<Product>();
            foreach (DataRow row in tableResult.Rows)
            {
                Product newEntry = new Product()
                {
                    ProductName = row["ProductName"].ToString(),
                    Period = DateTime.Parse(row["period"].ToString()).ToUniversalTime(),
                    DeflectionPercentage = Convert.ToDouble(row["deflection"])
                };

                products.Add(newEntry);
            }

            var productsGroup = products.GroupBy(p => p.ProductName);

            foreach (var grp in productsGroup)
            {
                ChartGeneratorPostBody postBody = new ChartGeneratorPostBody();
                postBody.XAxis.Type = "string";
                postBody.XAxis.Name = "Week";
                postBody.YAxis.Type = "double";
                postBody.YAxis.Name = "Deflection(%)";

                var orderedList = grp.OrderBy(p => p.Period);
                foreach (var element in orderedList)
                {
                    postBody.XAxis.Values.Add($"{element.Period.Month}/{element.Period.Day}");
                    postBody.YAxis.Values.Add(element.DeflectionPercentage);
                }

                postBody.Chart.Color = new RGBColor(83, 186, 226);
                postBody.Chart.Height = 150;
                postBody.Chart.Width = 450;
                postBody.Chart.ChartType = "area";

                string chartContent = ChartClient.GetChartContent(postBody);
                if (!string.IsNullOrWhiteSpace(chartContent))
                {
                    Image img = new Image()
                    {
                        Cid = Regex.Replace(grp.Key, "[^a-zA-Z]", string.Empty).ToLower(),
                        ContentBase64Encoded = chartContent
                    };

                    output.Add(new Tuple<string, Image>(grp.Key, img));
                }
            }

            return output;
        }

        internal static List<Category> GetCategoryBreakdownData(KustoClient kustoClient, string query)
        {
            var tableResult = kustoClient.ExecuteQueryAsync(query, "waws-prod-blu-000").Result;
            List<Category> categories = new List<Category>();
            if (tableResult == null || tableResult.Rows == null || tableResult.Rows.Count <= 0)
            {
                return categories;
            }

            foreach (DataRow row in tableResult.Rows)
            {
                Category newEntry = new Category()
                {
                    Period = DateTime.Parse(row["period"].ToString()).ToUniversalTime(),
                    Name = row["SupportTopicL2"].ToString(),
                    CasesDeflected = Convert.ToInt32(row["casesDeflected"]),
                    CasesLeaked = Convert.ToInt32(row["casesLeaked"]),
                    DeflectionPercentage = Convert.ToDouble(row["deflection"])
                };

                categories.Add(newEntry);
            }

            return categories;
        }

        internal static List<SubCategory> GetSubCategoryBreakdownData(KustoClient kustoClient, string query)
        {
            var tableResult = kustoClient.ExecuteQueryAsync(query, "waws-prod-blu-000").Result;
            List<SubCategory> subCategories = new List<SubCategory>();
            if (tableResult == null || tableResult.Rows == null || tableResult.Rows.Count <= 0)
            {
                return subCategories;
            }

            foreach (DataRow row in tableResult.Rows)
            {
                SubCategory newEntry = new SubCategory()
                {
                    Name = row["SupportTopicL3"].ToString(),
                    SupportTopicId = row["SupportTopicId"].ToString(),
                    Period = DateTime.Parse(row["period"].ToString()).ToUniversalTime(),
                    CategoryName = row["SupportTopicL2"].ToString(),
                    CasesDeflected = Convert.ToInt32(row["casesDeflected"]),
                    CasesLeaked = Convert.ToInt32(row["casesLeaked"]),
                    DeflectionPercentage = Convert.ToDouble(row["deflection"])
                };

                subCategories.Add(newEntry);
            }

            return subCategories;
        }

        internal static List<Solution> GetSolutionsData(KustoClient kustoClient, string query)
        {
            var tableResult = kustoClient.ExecuteQueryAsync(query, "waws-prod-blu-000").Result;
            List<Solution> solutions = new List<Solution>();
            if (tableResult == null || tableResult.Rows == null || tableResult.Rows.Count <= 0)
            {
                return solutions;
            }

            foreach (DataRow row in tableResult.Rows)
            {
                Solution newEntry = new Solution()
                {
                    Name = row["Name"].ToString(),
                    ProductName = row["ProductName"].ToString()
                };

                var currentNumerator = Convert.ToDouble(row["CurrentNumerator"]);
                var currentDenominator = Convert.ToDouble(row["CurrentDenominator"]);
                var currentDeflection = Convert.ToDouble(row["Current"]);
                var previousDeflection = Convert.ToDouble(row["Previous"]);

                var percentDiff = Math.Round(currentDeflection - previousDeflection, 2);

                newEntry.CurrentDeflection = $"{currentDeflection} %  ({currentNumerator}/{currentDenominator})";

                newEntry.ChangesFromLastPeriod = "-";

                if (percentDiff > 0)
                {
                    newEntry.ChangesFromLastPeriod = $"+{percentDiff.ToString()} %  &#8593;";
                }
                else if (percentDiff < 0)
                {
                    newEntry.ChangesFromLastPeriod = $"{percentDiff.ToString()} %  &#8595;";
                }

                solutions.Add(newEntry);
            }

            return solutions;
        }

        internal static string GetP360ProductLink(string productId)
        {
            switch (productId.ToLower())
            {
                case "14748":
                    return "https://product360.msftcloudes.com/product/134/domain/Support?meter=Microsoft.Cloud.P360.SupportDomain.SelfHelp.Production.Deflection_36344&period=Week";
                case "16170":
                    return "https://product360.msftcloudes.com/product/252/domain/Support?meter=Microsoft.Cloud.P360.SupportDomain.SelfHelp.Production.Deflection_36374&period=Week";
                case "16333":
                    return "https://product360.msftcloudes.com/product/253/domain/Support?meter=Microsoft.Cloud.P360.SupportDomain.SelfHelp.Production.Deflection_36375&period=Week";
                case "16072":
                    return "https://product360.msftcloudes.com/product/251/domain/Support?meter=Microsoft.Cloud.P360.SupportDomain.SelfHelp.Production.Deflection_36373&period=Week";
                default:
                    return null;
            }
        }
    }
}
