using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;

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
                string wildCardQuery = string.Join("or", wildCardHostNames.Select(w => $@" {hostNameColumn} endswith ""{w}"""));
                hostNameQuery = $"{hostNameQuery} or {wildCardQuery}";
            }

            return hostNameQuery;
        }

        /// <summary>
        /// Filter the given table to get site events for the given slot name.
        /// </summary>
        /// <param name="slotName">SlotName</param>
        /// <param name="slotTimeRanges">Runtime site slot map obtained from observer</param>
        /// <param name="siteEventsTable">DataTable of web app events that atleast has a site name column and a timestamp column</param>
        /// <param name="siteColumnName">Name of column for site name</param>
        /// <param name="timeStampColumnName">Name of column for timestamp</param>
        /// <example>
        /// <code>
        /// public async static Task<![CDATA[Response]]> Run(DataProviders dp, <![CDATA[OperationContext<App> cxt]]>, Response res){
        ///     var siteEventsDataTable = await dp.Kusto.ExecuteQuery(runtimeWorkerEventsQuery, cxt.Resource.Stamp.InternalName));
        ///     var slotTimeRanges = await dp.Observer.GetRuntimeSiteSlotMap(cxt.Resource.Stamp.InternalName, cxt.Resource.Name);
        ///     var webAppStagingEvents = Utilities.GetSlotEvents(cxt.Resource.Slot, slotTimeRanges, siteEventsDataTable, "SiteName", "TIMESTAMP").
        /// }
        /// </code>
        /// </example>
        public static DataTable GetSlotEvents(string slotName, Dictionary<string, List<RuntimeSitenameTimeRange>> slotTimeRanges, DataTable siteEventsTable, string siteColumnName = "SiteName", string timeStampColumnName = "TIMESTAMP")
        {
            var dt = new DataTable();
            var columns = siteEventsTable.Columns.Cast<DataColumn>().Select(dc => new DataColumn(dc.ColumnName, dc.DataType, dc.Expression, dc.ColumnMapping)).ToArray();
            List<RuntimeSitenameTimeRange> timeRangeForSlot;

            if (slotTimeRanges.ContainsKey(slotName.ToLower()))
            {
                timeRangeForSlot = slotTimeRanges[slotName];
            }
            else
            {
                throw new ArgumentException($"{slotName} is not an existing slot for this site. ");
            }

            try
            {
                dt.Columns.AddRange(columns);

                foreach (DataRow row in siteEventsTable.Rows)
                {
                    var siteName = (string)row[siteColumnName];
                    var timeStamp = (DateTime)row[timeStampColumnName];

                    foreach (var slotTimeRangeInfo in timeRangeForSlot)
                    {
                        if (siteName.Equals(slotTimeRangeInfo.RuntimeSitename, StringComparison.CurrentCultureIgnoreCase) && timeStamp >= slotTimeRangeInfo.StartTime && timeStamp <= slotTimeRangeInfo.EndTime)
                        {
                            dt.Rows.Add(row.ItemArray);
                            break;
                        }
                    }
                }
            }catch(Exception ex)
            {
                throw new Exception("Failed to get runtime slotmap table.", ex);
            }

            return dt;
        }

        /// <summary>
        /// Gets the form input object for the given form and input id
        /// </summary>
        /// <param name="form">Form to search</param>
        /// <param name="inputId">Given input id</param>
        public static FormInputBase GetFormInput(Form form, int inputId)
        {
            try
            {
                var formInput = form.FormInputs.First(i => i.InputId == inputId);
                return formInput;
            }
            catch (Exception e)
            {
                throw new Exception($"{e.Message}, Please check the given input id");
            }
        }

        /// <summary>
        /// Gets the button id of the button currently executing
        /// </summary>
        public static int GetExecutingButtonId(Form form)
        {
            try
            {
                var executingButtonId = form.FormInputs.Find(input => input.InputType == FormInputTypes.Button);
                return executingButtonId.InputId;
            }
            catch(Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// Returns all the form inputs filtered by the <paramref name="filterInputType"/>
        /// </summary>
        public IEnumerable<FormInputBase> GetFormInputsByType(Form form, FormInputTypes filterInputType)
        {
            return form.FormInputs.Where(input => input.InputType == filterInputType);
        }
    }
}
