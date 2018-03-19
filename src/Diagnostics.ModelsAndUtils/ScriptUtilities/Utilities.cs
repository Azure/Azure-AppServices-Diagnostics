using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Diagnostics.ModelsAndUtils
{
    public class Utilities
    {
        public static string TimeFilterQuery(OperationContext cxt, string timeColumnName = "PreciseTimeStamp")
        {
            return $"{timeColumnName} >= datetime({cxt.StartTime}) and {timeColumnName} <= datetime({cxt.EndTime})";
        }

        public static string TenantFilterQuery(OperationContext cxt)
        {
            return $"Tenant in ({string.Join(",", cxt.Resource.TenantIdList.Select(t => $@"""{t}"""))})";
        }

        public static string TimeAndTenantFilterQuery(OperationContext cxt, string timeColumnName = "PreciseTimeStamp")
        {
            return $"{TimeFilterQuery(cxt, timeColumnName)} and {TenantFilterQuery(cxt)}";
        }

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
