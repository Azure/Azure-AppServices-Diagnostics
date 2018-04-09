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
        /// <param name="cxt"><see cref="OperationContext"/></param>
        /// <param name="timeColumnName">Name of the Time Column</param>
        /// <returns>Kusto time filter query part</returns>
        /// <example> 
        /// This sample shows how to use <see cref="TimeFilterQuery"/> method.
        /// <code>
        /// public string GetKustoQuery(OperationContext cxt)
        /// {
        ///     string timeFilterQueryPart = Utilities.TimeFilterQuery(cxt);
        ///     return $"SomeTableName | where {timeFilterQueryPart}";
        /// }
        /// </code>
        /// This will produce "SomeTableName | where <![CDATA[PreciseTimeStamp >= datetime(2018-04-04 04:10) and PreciseTimeStamp <= datetime(2018-04-05 04:10) ]]>"
        /// </example>
        public static string TimeFilterQuery(OperationContext cxt, string timeColumnName = "PreciseTimeStamp")
        {
            return $"{timeColumnName} >= datetime({cxt.StartTime}) and {timeColumnName} <= datetime({cxt.EndTime})";
        }

        /// <summary>
        /// Creates a kusto query part for Tenant filters. This is a recommended way to use Tenant filters as this takes care of mega-stamps also.
        /// </summary>
        /// <param name="cxt"><see cref="OperationContext"/></param>
        /// <returns>Kusto tenant filter query part</returns>
        /// <example>
        /// <code>
        /// public string GetKustoQuery(OperationContext cxt)
        /// {
        ///     string tenantFilterQueryPart = Utilities.TenantFilterQuery(cxt);
        ///     return $"SomeTableName | where {tenantFilterQueryPart}";
        /// }
        /// </code>
        /// This will produce "SomeTableName | where <![CDATA[Tenant in ("tenant-guid-1", "tenant-guid-2") ]]>"
        /// </example>
        public static string TenantFilterQuery(OperationContext cxt)
        {
            return $"Tenant in ({string.Join(",", cxt.Resource.TenantIdList.Select(t => $@"""{t}"""))})";
        }

        /// <summary>
        /// Creates a kusto query part for Time and Tenant filter together.
        /// </summary>
        /// <param name="cxt"><see cref="OperationContext"/></param>
        /// <param name="timeColumnName">Name of the Time Column</param>
        /// <returns>Kusto query part</returns>
        /// <example>
        /// <code>
        /// public string GetKustoQuery(OperationContext cxt)
        /// {
        ///     string queryPart = Utilities.TimeAndTenantFilterQuery(cxt);
        ///     return $"SomeTableName | where {queryPart}";
        /// }
        /// </code>
        /// This will produce "SomeTableName | where <![CDATA[PreciseTimeStamp >= datetime(2018-04-04 04:10) and PreciseTimeStamp <= datetime(2018-04-05 04:10) and Tenant in ("tenant-guid-1", "tenant-guid-2") ]]>"
        /// </example>
        public static string TimeAndTenantFilterQuery(OperationContext cxt, string timeColumnName = "PreciseTimeStamp")
        {
            return $"{TimeFilterQuery(cxt, timeColumnName)} and {TenantFilterQuery(cxt)}";
        }

        /// <summary>
        /// Creates kusto query part to cover hostnames. Takes care for wildcard hostnames also.
        /// </summary>
        /// <param name="cxt"><see cref="OperationContext"/></param>
        /// <param name="hostNameColumn">Hostname column name</param>
        /// <returns>Kusto query part.</returns>
        /// <example>
        /// <code>
        /// public string GetKustoQuery(OperationContext cxt)
        /// {
        ///     string queryPart = Utilities.TimeAndTenantFilterQuery(cxt);
        ///     return $"SomeTableName | where {queryPart}";
        /// }
        /// </code>
        /// This will produce "SomeTableName | where <![CDATA[where Cs_host in ("somdomain.azurewebsites.net", "somedomain.com") ]]>"
        /// In case of wildcard domain like *.somedomain.com,  this will produce "SomeTableName | where <![CDATA[Cs_host in ("somdomain.azurewebsites.net") or Cs_host endswith "somedomain.com" ]]>"
        /// </example>
        public static string HostNamesFilterQuery(OperationContext cxt, string hostNameColumn = "Cs_host")
        {
            var wildCardHostNames = cxt.Resource.HostNames.Where(p => p.StartsWith("*"));
            var nonWildCardHostNames = cxt.Resource.HostNames.Where(p => !p.StartsWith("*"));

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
