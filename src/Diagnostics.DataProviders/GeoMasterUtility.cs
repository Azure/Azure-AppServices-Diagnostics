using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Diagnostics.DataProviders
{
    public class GeoMasterConstants
    {
        public const string August2016Version = "2016-08-01";
        public const string ProductionSlot = "Production";
        public const string SlotNameTemplateParameter = "/{slot}";
        public const string SlotsResource = "/slots";
        public const string NameTemplateParamater = "/{name}";
        public const string SitesResource = "sites";
        public const string Functions = "functions";
        public const string ResourceProviderName = "Microsoft.Web";
        public const string NetworkResourceProviderName = "Microsoft.Network";
        public const string ResourceProviderDisplayName = "Microsoft Web Apps";
        public const string ResourceGroupSegment = "resourceGroups";
        public const string ResourceGroupRoot = "subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}";
        public const string ResourceRoot =
                "subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/" + ResourceProviderName;
    }

    public static class SitePathUtility
    {
        public static string GetSitePath(string subscriptionId, string resourceGroupName, string name = null)
        {
            var path = GetResourceRootPath(subscriptionId, resourceGroupName) + "/" + GeoMasterConstants.SitesResource;

            if (!string.IsNullOrWhiteSpace(name) )
            {
                SiteNameParser.ParseSiteWithSlotName(name, out string siteName, out string slotName);

                path += GeoMasterConstants.NameTemplateParamater.NamedFormat(new Dictionary<string, string>
                {
                    {"name", siteName}
                });

                if (!string.Equals(slotName, GeoMasterConstants.ProductionSlot, StringComparison.OrdinalIgnoreCase))
                {
                    path += GeoMasterConstants.SlotsResource + GeoMasterConstants.SlotNameTemplateParameter.NamedFormat(new Dictionary<string, string>
                    {
                        {"slot", slotName}
                    });
                }
            }

            return path;
        }

        public static string GetResourceRootPath(string subscriptionId, string resourceGroupName)
        {
            return GeoMasterConstants.ResourceRoot.NamedFormat(new Dictionary<string, string>
            {
                {"subscriptionId", subscriptionId},
                {"resourceGroupName", resourceGroupName}
            });
        }
        public static string CsmAnnotateQueryString(string queryString, string apiVersion)
        {
            var nameValues = HttpUtility.ParseQueryString(queryString);
            var dict = nameValues.Cast<string>().ToDictionary(k => k, k => nameValues[k]);
            dict["api-version"] = apiVersion;

            return "?" + string.Join("&", from kvp in dict
                                          where kvp.Value != null
                                          select string.Format("{0}={1}", HttpUtility.UrlEncode(kvp.Key), HttpUtility.UrlEncode(kvp.Value)));


        }
    }

    public static class SiteNameParser
    {
        private static readonly Regex SiteWithSlotNameRegexp =
            new Regex(@"^(?<siteName>[^\(]+)\((?<slotName>[^\)]+)\)$");
        private const string FmtSiteWithSlotName = "{0}({1})";
        private const string FmtHostPrefixWithSlotName = "{0}-{1}";
        private const string FmtHostname = "{0}.{1}";


        public static void ParseSiteWithSlotName(string siteWithSlotName, out string siteName, out string slotName)
        {
            var match = SiteWithSlotNameRegexp.Match(siteWithSlotName);
            if (match.Success)
            {
                siteName = match.Groups["siteName"].Value;
                slotName = match.Groups["slotName"].Value;
            }
            else
            {
                siteName = siteWithSlotName;
                slotName = GeoMasterConstants.ProductionSlot;
            }
        }

        public static string GenerateSiteWithSlotName(string siteName, string slotName)
        {
            if (!string.IsNullOrEmpty(slotName) && !string.Equals(slotName, GeoMasterConstants.ProductionSlot, StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(FmtSiteWithSlotName, siteName, slotName);
            }

            return siteName;
        }

        public static string GenerateHostPrefixWithSlotName(string siteName, string slotName)
        {
            if (!string.Equals(slotName, GeoMasterConstants.ProductionSlot, StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(FmtHostPrefixWithSlotName, siteName, slotName);
            }

            return siteName;
        }

        /// <summary>
        /// This will convert a site name from the format in db to a hostname e.g abc(staging) and dnsSuffix = xyz.com becomes abc-staging.xyz.com
        /// </summary>
        /// <param name="siteWithSlotName">site with slot name</param>
        /// <param name="dnsSuffix">DNS suffix for hostname</param>
        /// <returns></returns>
        public static string GenerateHostNameFromSiteName(string siteWithSlotName, string dnsSuffix)
        {
            string slotName;
            ParseSiteWithSlotName(siteWithSlotName, out string siteName, out slotName);
            return string.Format(FmtHostname, GenerateHostPrefixWithSlotName(siteName, slotName), dnsSuffix);
        }
    }

    public static class ExtensionMethods
    {
        public static string NamedFormat(this string inputString, IDictionary<string, string> replacements)
        {
            return replacements.Aggregate(inputString, (intermediate, kvp) => intermediate.Replace("{" + kvp.Key + "}", kvp.Value));
        }

        public static T CastTo<T>(this object obj)
        {
            return (T)obj;
        }
    }
}
