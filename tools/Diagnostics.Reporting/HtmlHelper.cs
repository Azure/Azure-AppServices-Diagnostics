using Diagnostics.Reporting.Models;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Diagnostics.Reporting
{
    internal static class HtmlHelper
    {
        internal static string GetProductMetricsTable(List<Product> products, List<Tuple<string, Image>> weeklyTrends, ref SendGridMessage sendGridMessage)
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
                    var percentDiff = items.ElementAt(0).CasesDeflected - items.ElementAt(1).CasesDeflected;
                    if (percentDiff > 0)
                    {
                        changesSinceLast = $"+{percentDiff.ToString()}  &#8593;";
                        color = "green";
                    }
                    else
                    {
                        changesSinceLast = $"{percentDiff.ToString()}  &#8595;";
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

                string imgTag = string.Empty;

                if (weeklyTrends != null)
                {
                    var productWeeklyTrend = weeklyTrends.FirstOrDefault(p => p.Item1.Equals(grp.Key, StringComparison.OrdinalIgnoreCase));

                    if (productWeeklyTrend != null)
                    {
                        sendGridMessage.AddAttachment($"{productWeeklyTrend.Item2.Cid}.png",
                            productWeeklyTrend.Item2.ContentBase64Encoded,
                            "image/jpeg",
                            "inline",
                            productWeeklyTrend.Item2.Cid);

                        imgTag = $@"<img src=cid:{productWeeklyTrend.Item2.Cid} />";
                    }
                }

                string productRow = $@"
                <tr>
                    <td>{grp.Key}</td>
                    <td>{casesDeflected}</td>
                    <td>{casesLeaked}</td>
                    <td>{deflectionPercentage}</td>
                    <td style=""color:{color}"">{changesSinceLast}</td>
                    <td>{imgTag}</td>
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

            var grp = solutions.GroupBy(p => p.ProductName);

            foreach (var grpEntry in grp)
            {
                string asdColumn = "-";
                string selfHelpColumn = "-";
                string torrontoColumn = "-";

                foreach (var solution in grpEntry)
                {
                    string color = "red";
                    if (solution.ChangesFromLastPeriod.Contains("+"))
                    {
                        color = "green";
                    }
                    else if (solution.ChangesFromLastPeriod.Equals("-"))
                    {
                        color = "lightgrey";
                    }

                    string column = $@"{solution.CurrentDeflection}    <span style=""color:{color};margin-left:10px"">{solution.ChangesFromLastPeriod}</span>";

                    if (solution.Name.Equals("Diagnostic", StringComparison.OrdinalIgnoreCase))
                    {
                        asdColumn = column;
                    }
                    else if (solution.Name.Equals("CommonSolutions", StringComparison.OrdinalIgnoreCase))
                    {
                        selfHelpColumn = column;
                    }
                    else
                    {
                        torrontoColumn = column;
                    }
                }

                string solutionRow = $@"
                        <tr>
                        <td>{grpEntry.Key}</td>
                        <td>{asdColumn}</td>
                        <td>{selfHelpColumn}</td>
                        <td>{torrontoColumn}</td>
                        </tr>";

                rows += solutionRow;
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
                    var percentDiff = category.CasesDeflected - previousEntry.First().CasesDeflected;
                    if (percentDiff > 0)
                    {
                        changesSinceLast = $"+{percentDiff.ToString()}  &#8593;";
                        color = "green";
                    }
                    else
                    {
                        changesSinceLast = $"{percentDiff.ToString()}  &#8595;";
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
                    <td>{casesDeflected}</td>
                    <td>{casesLeaked}</td>
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
            string emailTemplate = File.ReadAllText(@"EmailTemplates\Parts\SubCategoryBreakdownTable.html");
            foreach (var categoryGroup in groups)
            {
                string subCategoryTable = emailTemplate;
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

                            var percentDiff = firstGrp.CasesDeflected - previousEntry.CasesDeflected;
                            if (percentDiff > 0)
                            {
                                changesSinceLast = $"+{percentDiff.ToString()}  &#8593;";
                                color = "green";
                            }
                            else
                            {
                                changesSinceLast = $"{percentDiff.ToString()}  &#8595;";
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
                                <td>{casesDeflected}</td>
                                <td>{casesLeaked}</td>
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
