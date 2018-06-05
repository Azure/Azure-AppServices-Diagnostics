using Diagnostics.DataProviders;
using Diagnostics.Reporting.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Diagnostics.Reporting
{
    internal class Helper
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

            foreach(DataRow row in tableResult.Rows)
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

                var percentDiff = currentDeflection - previousDeflection;

                newEntry.CurrentDeflection = $"{currentDeflection} %  ({currentNumerator}/{currentDenominator})";

                newEntry.ChangesFromLastPeriod = "-";

                if (percentDiff > 0)
                {
                    newEntry.ChangesFromLastPeriod = $"+{percentDiff.ToString()} %  &#8593;";
                }
                else
                {
                    newEntry.ChangesFromLastPeriod = $"{percentDiff.ToString()} %  &#8595;";
                }

                solutions.Add(newEntry);
            }

            return solutions;
        }

        internal static string GetP360Link(string productId)
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

        internal static string GetProductMetricsTable(List<Product> products)
        {
            string htmlPart = File.ReadAllText(@"EmailTemplates\Parts\OverallProductMetricsTable.html");
            string rows = "";
            var groups = products.GroupBy(p => p.ProductName);
            foreach (var grp in groups)
            {
                var items = grp.OrderByDescending(g => g.Period);
                string casesLeaked = items.First().CasesLeaked.ToString();
                string casesDeflected = items.First().CasesDeflected.ToString();
                string deflectionPercentage = items.First().DeflectionPercentage + " %";

                string changesSinceLast = "-";
                string color = "grey";
                if (items.Count() >= 2)
                {
                    var percentDiff = items.ElementAt(0).DeflectionPercentage - items.ElementAt(1).DeflectionPercentage;
                    if (percentDiff > 0)
                    {
                        changesSinceLast = $"+{percentDiff.ToString()} %  &#8593;";
                        color = "green";
                    }
                    else
                    {
                        changesSinceLast = $"{percentDiff.ToString()} %  &#8595;";
                        color = "red";
                    }

                    var casesLeakedDiff = items.ElementAt(0).CasesLeaked - items.ElementAt(1).CasesLeaked;
                    if (casesLeakedDiff > 0)
                    {
                        casesLeaked += $" ( +{casesLeakedDiff} &#8593; )";
                    }
                    else
                    {
                        casesLeaked += $" ( {casesLeakedDiff} &#8595; )";
                    }
                }

                string p360linkCol = string.IsNullOrWhiteSpace(items.First().P360Link) ? "" : $@"<a target=""_blank"" href=""{items.First().P360Link}"">Weekly Trends in P360</a></td>";

                string productRow = $@"
                <tr>
                    <td>{grp.Key}</td>
                    <td>{casesLeaked}</td>
                    <td>{casesDeflected}</td>
                    <td>{deflectionPercentage}</td>
                    <td style=""color:{color}"">{changesSinceLast}</td>
                    <td>{p360linkCol}</td>
                </tr>";

                rows += productRow;
            }

            return htmlPart.Replace("{Rows}", rows);
        }
        
        internal static string GetSolutionMetricsTable(List<Solution> solutions)
        {
            string htmlPart = File.ReadAllText(@"EmailTemplates\Parts\SolutionBreakdownTable.html");
            string rows = "";

            if(solutions != null && solutions.Any())
            {
                foreach(var solution in solutions)
                {
                    string solutionName = solution.Name;
                    if (solutionName.Equals("Diagnostic", StringComparison.OrdinalIgnoreCase))
                    {
                        solutionName = "App Service Diagnostics";
                    }
                    else if(solutionName.Equals("CommonSolutions", StringComparison.OrdinalIgnoreCase))
                    {
                        solutionName = "Self-Help Static Content";
                    }

                    string color = "red";
                    if (solution.ChangesFromLastPeriod.Contains("+"))
                    {
                        color = "green";
                    }


                    string solutionRow = $@"
                        <tr>
                        <td>{solution.ProductName}</td>
                        <td>{solutionName}</td>
                        <td>{solution.CurrentDeflection}</td>
                        <td style=""color:{color}"">{solution.ChangesFromLastPeriod}</td>
                        </tr>";

                    rows += solutionRow;
                }
            }

            return htmlPart.Replace("{Rows}", rows);
        }
        
        internal static string GetCategoryBreakdownMetricsTable(List<Category> categories)
        {
            string htmlPart = File.ReadAllText(@"EmailTemplates\Parts\CategoryBreakdownTable.html");
            string rows = "";
            var groups = categories.GroupBy(p => p.Period).OrderByDescending(p => p.Key);

            var firstGroup = groups.First();
            var secondGroup = groups.Last();

            foreach (var category in firstGroup)
            {
                string categoryName = category.Name;
                string casesLeaked = category.CasesLeaked.ToString();
                string casesDeflected = category.CasesDeflected.ToString();
                string deflectionPercentage = category.DeflectionPercentage + " %";

                string changesSinceLast = "-";
                string color = "grey";

                var previousEntry = secondGroup.Where(p => p.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                if (previousEntry != null && previousEntry.Any())
                {
                    var percentDiff = category.DeflectionPercentage - previousEntry.First().DeflectionPercentage;
                    if (percentDiff > 0)
                    {
                        changesSinceLast = $"+{percentDiff.ToString()} %  &#8593;";
                        color = "green";
                    }
                    else
                    {
                        changesSinceLast = $"{percentDiff.ToString()} %  &#8595;";
                        color = "red";
                    }

                    var casesLeakedDiff = category.CasesLeaked - previousEntry.First().CasesLeaked;
                    if (casesLeakedDiff > 0)
                    {
                        casesLeaked += $" ( +{casesLeakedDiff} &#8593; )";
                    }
                    else
                    {
                        casesLeaked += $" ( {casesLeakedDiff} &#8595; )";
                    }
                }

            string categoryRow = $@"
                <tr>
                    <td>{categoryName}</td>
                    <td>{casesLeaked}</td>
                    <td>{casesDeflected}</td>
                    <td>{deflectionPercentage}</td>
                    <td style=""color:{color}"">{changesSinceLast}</td>
                </tr>";

                rows += categoryRow;
            }

            return htmlPart.Replace("{Rows}", rows);
        }

        internal static string GetSubCategoryBreakdownMetricsTable(List<SubCategory> subCategories)
        {
            string htmlPart = string.Empty;
            DateTime latestPeriod = subCategories.OrderByDescending(p => p.Period).First().Period;
            var groups = subCategories.GroupBy(p => p.CategoryName);

            foreach(var categoryGroup in groups)
            {
                string subCategoryTable = File.ReadAllText(@"EmailTemplates\Parts\SubCategoryBreakdownTable.html");
                subCategoryTable = subCategoryTable.Replace(@"{Category}", categoryGroup.Key);

                var subCategoryGroup = categoryGroup.GroupBy(p => p.Name);

                string rows = "";
                foreach (var subCat in subCategoryGroup)
                {
                    var periodGroups = subCat.OrderByDescending(q => q.Period);
                    var firstGrp = periodGroups.First();
                    if (firstGrp.Period >= latestPeriod)
                    {
                        string casesLeaked = firstGrp.CasesLeaked.ToString();
                        string casesDeflected = firstGrp.CasesDeflected.ToString();
                        string deflectionPercentage = firstGrp.DeflectionPercentage + " %";

                        string changesSinceLast = "-";
                        string color = "grey";

                        if (periodGroups.Count() >= 2)
                        {
                            var previousEntry = periodGroups.ElementAt(1);

                            var percentDiff = firstGrp.DeflectionPercentage - previousEntry.DeflectionPercentage;
                            if (percentDiff > 0)
                            {
                                changesSinceLast = $"+{percentDiff.ToString()} %  &#8593;";
                                color = "green";
                            }
                            else
                            {
                                changesSinceLast = $"{percentDiff.ToString()} %  &#8595;";
                                color = "red";
                            }

                            var casesLeakedDiff = firstGrp.CasesLeaked - previousEntry.CasesLeaked;
                            if (casesLeakedDiff > 0)
                            {
                                casesLeaked += $" ( +{casesLeakedDiff} &#8593; )";
                            }
                            else
                            {
                                casesLeaked += $" ( {casesLeakedDiff} &#8595; )";
                            }
                        }

                        rows += $@"
                            <tr>
                                <td>{subCat.Key}</td>
                                <td>{casesLeaked}</td>
                                <td>{casesDeflected}</td>
                                <td>{deflectionPercentage}</td>
                                <td style=""color:{color}"">{changesSinceLast}</td>
                            </tr>";
                    }
                }
                

                htmlPart += subCategoryTable.Replace("{Rows}", rows);
            }

            return htmlPart;
        }
    }
}