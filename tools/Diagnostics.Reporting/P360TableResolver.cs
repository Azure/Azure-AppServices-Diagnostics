using Diagnostics.DataProviders;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Diagnostics.Reporting
{
    public static class P360TableResolver
    {
        private static string _supportProductionDeflectionWeeklyTable { get; set; }

        private static string _supportProductionDeflectionWeeklyPoPInsightsTable { get; set; }

        private static string _query = @".show tables
            | where TableName startswith ""{Name}""
            | top 1 by TableName desc nulls last 
            | project TableName";

        public static string SupportProductionDeflectionWeeklyTable => _supportProductionDeflectionWeeklyTable;

        public static string SupportProductionDeflectionWeeklyPoPInsightsTable => _supportProductionDeflectionWeeklyPoPInsightsTable;
        
        public static void Init(KustoClient ks)
        {
            DataTable dt1 = ks.ExecuteQueryAsync(_query.Replace("{Name}", "SupportProductionDeflectionWeeklyVer"), "waws-prod-blu-000").Result;
            if(dt1 != null && dt1.Rows != null && dt1.Rows.Count > 0)
            {
                _supportProductionDeflectionWeeklyTable = dt1.Rows[0][0].ToString();
            }

            DataTable dt2 = ks.ExecuteQueryAsync(_query.Replace("{Name}", "SupportProductionDeflectionWeeklyPoPInsightsVer"), "waws-prod-blu-000").Result;
            if (dt2 != null && dt2.Rows != null && dt2.Rows.Count > 0)
            {
                _supportProductionDeflectionWeeklyPoPInsightsTable = dt2.Rows[0][0].ToString();
            }
        }
    }
}
