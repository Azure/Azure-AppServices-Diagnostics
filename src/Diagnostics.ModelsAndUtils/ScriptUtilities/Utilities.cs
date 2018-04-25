using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Diagnostics.ModelsAndUtils.ScriptUtilities
{
    public class Utilities
    {
        /// <summary>
        /// Creates a kusto query part with time filters
        /// </summary>
        /// <param name="startTime">start time</param>
        /// <param name="endTime">end time</param>
        /// <param name="timeColumnName">Name of the Time Column</param>
        /// <returns>Kusto time filter query part</returns>
        /// <example> 
        /// This sample shows how to use <see cref="TimeFilterQuery"/> method.
        /// <code>
        /// public string GetKustoQuery(<![CDATA[OperationContext<App> cxt]]>)
        /// {
        ///     string timeFilterQueryPart = Utilities.TimeFilterQuery(cxt.StartTime, cxt.EndTime);
        ///     return $"SomeTableName | where {timeFilterQueryPart}";
        /// }
        /// </code>
        /// This will produce "SomeTableName | where <![CDATA[PreciseTimeStamp >= datetime(2018-04-04 04:10) and PreciseTimeStamp <= datetime(2018-04-05 04:10) ]]>"
        /// </example>
        public static string TimeFilterQuery(string startTime, string endTime, string timeColumnName = "PreciseTimeStamp")
        {
            return $"{timeColumnName} >= datetime({startTime}) and {timeColumnName} <= datetime({endTime})";
        }

        /// <summary>
        /// Creates a kusto query part for Tenant filters. This is a recommended way to use Tenant filters as this takes care of mega-stamps also.
        /// </summary>
        /// <param name="resource">Resource Object</param>
        /// <returns>Kusto tenant filter query part</returns>
        /// <example>
        /// <code>
        /// public string GetKustoQuery(<![CDATA[OperationContext<App> cxt]]>)
        /// {
        ///     string tenantFilterQueryPart = Utilities.TenantFilterQuery(cxt.Resource);
        ///     return $"SomeTableName | where {tenantFilterQueryPart}";
        /// }
        /// </code>
        /// This will produce "SomeTableName | where <![CDATA[Tenant in ("tenant-guid-1", "tenant-guid-2") ]]>"
        /// </example>
        public static string TenantFilterQuery(IResource resource)
        {
            List<string> tenantIds = new List<string>();
            if(resource is App app)
            {
                tenantIds = app.Stamp.TenantIdList.ToList();
            }
            else if(resource is HostingEnvironment env)
            {
                tenantIds = env.TenantIdList.ToList();
            }

            return $"Tenant in ({string.Join(",", tenantIds.Select(t => $@"""{t}"""))})";
        }

        /// <summary>
        /// Creates a kusto query part for Time and Tenant filter together.
        /// </summary>
        /// <param name="startTime">Start Time</param>
        /// <param name="endTime">EndTime</param>
        /// <param name="resource">Resource</param>
        /// <param name="timeColumnName">Name of the Time Column</param>
        /// <returns>Kusto query part</returns>
        /// <example>
        /// <code>
        /// public string GetKustoQuery(<![CDATA[OperationContext<App> cxt]]>)
        /// {
        ///     string queryPart = Utilities.TimeAndTenantFilterQuery(cxt.StartTime, cxt.EndTime, cxt.Resource);
        ///     return $"SomeTableName | where {queryPart}";
        /// }
        /// </code>
        /// This will produce "SomeTableName | where <![CDATA[PreciseTimeStamp >= datetime(2018-04-04 04:10) and PreciseTimeStamp <= datetime(2018-04-05 04:10) and Tenant in ("tenant-guid-1", "tenant-guid-2") ]]>"
        /// </example>
        public static string TimeAndTenantFilterQuery(string startTime, string endTime, IResource resource, string timeColumnName = "PreciseTimeStamp")
        {
            return $"{TimeFilterQuery(startTime, endTime, timeColumnName)} and {TenantFilterQuery(resource)}";
        }

        /// <summary>
        /// Creates kusto query part to cover hostnames. Takes care for wildcard hostnames also.
        /// </summary>
        /// <param name="hostnames">List of hostnames</param>
        /// <param name="hostNameColumn">Hostname column name</param>
        /// <returns>Kusto query part.</returns>
        /// <example>
        /// <code>
        /// public string GetKustoQuery(<![CDATA[OperationContext<App> cxt]]>)
        /// {
        ///     string queryPart = Utilities.TimeAndTenantFilterQuery(cxt.Resource.Hostnames);
        ///     return $"SomeTableName | where {queryPart}";
        /// }
        /// </code>
        /// This will produce "SomeTableName | where <![CDATA[where Cs_host in ("somdomain.azurewebsites.net", "somedomain.com") ]]>"
        /// In case of wildcard domain like *.somedomain.com,  this will produce "SomeTableName | where <![CDATA[Cs_host in ("somdomain.azurewebsites.net") or Cs_host endswith "somedomain.com" ]]>"
        /// </example>
        public static string HostNamesFilterQuery(IEnumerable<string> hostnames, string hostNameColumn = "Cs_host")
        {
            var wildCardHostNames = hostnames.Where(p => p.StartsWith("*"));
            var nonWildCardHostNames = hostnames.Where(p => !p.StartsWith("*"));

            string hostNameQuery = string.Empty;

            if (nonWildCardHostNames.Any())
            {
                hostNameQuery = $"{hostNameColumn} in ({string.Join(",", nonWildCardHostNames.Select(h => $@"""{h}"""))})";
            }

            if (wildCardHostNames.Any())
            {
                string wildCardQuery = string.Join("or", wildCardHostNames.Select(w => $@"{hostNameColumn} endswith ""{w}"""));
                hostNameQuery = $"{hostNameQuery} or {wildCardQuery}";
            }

            return hostNameQuery;
        }
    }
}
