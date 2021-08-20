#load "DetectorUtils"
#load "CredentialTrapper"

using System.Linq;

private static string GetAutoHealingEventsQuery(OperationContext<App> cxt)
{
    return
    $@"AntaresRuntimeWorkerEvents 
    | where {Utilities.TimeAndTenantFilterQuery(cxt.StartTime, cxt.EndTime, cxt.Resource)}
    | where EventId == 62600 and (SiteName =~ '{cxt.Resource.Name}' or SiteName startswith '{cxt.Resource.Name}__')
    | project bin(PreciseTimeStamp, 5m), RoleInstance, SiteName, Trigger 
    | join kind= inner (
        RoleInstanceHeartbeat 
        | where {Utilities.TimeAndTenantFilterQuery(cxt.StartTime, cxt.EndTime, cxt.Resource)}
        | summarize MachineName = any(MachineName), InstanceId = any(InstanceId) by RoleInstance
        ) on RoleInstance
    | summarize Count=count() by PreciseTimeStamp, SiteName, Trigger
    | order by PreciseTimeStamp asc";
}

private static string GetAutoHealingEventsQueryForGraph(OperationContext<App> cxt)
{
    return
    GetAutoHealingEventsQuery(cxt) + " | evaluate pivot(Trigger, sum(Count))";
}

private static string GetDaasConsoleExecutions(OperationContext<App> cxt)
{
    
    return
    $@"AntaresRuntimeWorkerEvents
    | where {Utilities.TimeAndTenantFilterQuery(cxt.StartTime, cxt.EndTime, cxt.Resource)}
    | where SiteName =~ '{cxt.Resource.Name}' or SiteName startswith '{cxt.Resource.Name}__'
    | where EventId == 62500 or EventId == 62501 or EventId == 62502 or EventId == 62503
    | where EventMessage contains 'DaasConsole started with'
    | parse EventMessage with * 'DaasConsole started with -' DaasConsoleSwitch "" "" Diagnoser "" parameters"" *
    | extend DaasCommand = strcat(DaasConsoleSwitch, '(', Diagnoser, ')') 
    | summarize Count=count() by TIMESTAMP=bin(TIMESTAMP, 5m),  DaasCommand
    | order by TIMESTAMP asc";
}

private static string GetDaasConsoleExecutionsLatestBits(OperationContext<App> cxt)
{
    return
    $@"DaaS
    | where {Utilities.TimeAndTenantFilterQuery(cxt.StartTime, cxt.EndTime, cxt.Resource)}
    | where SiteName =~ '{cxt.Resource.Name}' or SiteName startswith '{cxt.Resource.Name}__'
    | where EventId == 1005
    | extend DaasConsoleDetails = parse_json(Details)
    | project TIMESTAMP, SiteName, Diagnoser = tostring(DaasConsoleDetails.Diagnoser)
    | summarize Count=count() by TIMESTAMP=bin(TIMESTAMP, 5m), SiteName, Diagnoser
    | order by TIMESTAMP asc";
}

private static string GetAutoHealInvocationFailures(OperationContext<App> cxt)
{
    return
    $@"AntaresWebWorkerEventLogs
    | where {Utilities.TimeAndTenantFilterQuery(cxt.StartTime, cxt.EndTime, cxt.Resource)}
    | where SiteName =~ '{cxt.Resource.Name}' or SiteName startswith '{cxt.Resource.Name}__'
    | where EventSource == 'IIS-Process-Monitoring'
    | where EventId == long(9022)
    | extend EventDetails = parse_xml(RawValue)
    | extend MessageDetails = tostring(EventDetails.Event.EventData.Data)
    | parse MessageDetails with * "" custom action '"" Action ""' due to '"" Reason ""'"" *  "". The error code is "" ErrorCode
    | summarize Failures=count() by TIMESTAMP=bin(TIMESTAMP,5m), SiteName, ErrorCode, Action, Reason
    | order by TIMESTAMP asc";
}

private static string GetDaasThrottles(OperationContext<App> cxt)
{
    return
    $@"DaaS
    | where {Utilities.TimeAndTenantFilterQuery(cxt.StartTime, cxt.EndTime, cxt.Resource)}
    | where SiteName =~ '{cxt.Resource.Name}' or SiteName startswith '{cxt.Resource.Name}__'
    | where ExceptionType == 'System.AccessViolationException' and EventId == long(1003)
    | summarize Rejections=count() by bin(TIMESTAMP,5m) , SiteName, ExceptionMessage";
}

public class DiagnosticConstants
{
    public const string PercentSlowRequests = "Percent Slow Requests";
    public const string TotalRequests = "Total Requests";
    public const string Memory = "Memory";
    public const string StatusCode = "Status Code";
    public const string SlowRequests = "Slow Requests";
    public const string PercentMemory = "Percent Memory";
}

[AppFilter(AppType = AppType.All, PlatformType = PlatformType.Windows, StackType = StackType.All, InternalOnly = false)]
[Definition(Id = "AutoHeal", Name = "Auto-Heal History", Author = "juzhu;puneetg", Description = "The below view shows you the times autohealing was triggered for your app")]
public async static Task<Response> Run(DataProviders dp, OperationContext<App> cxt, Response res)
{
    // this is auto heal detector. 
    var daasConsoleTask = dp.Kusto.ExecuteQuery(GetDaasConsoleExecutions(cxt), cxt.Resource.Stamp.InternalName, null, "GetDaasConsoleExecutions");
    var daasConsoleLatestTask = dp.Kusto.ExecuteQuery(GetDaasConsoleExecutionsLatestBits(cxt), cxt.Resource.Stamp.InternalName, null, "GetDaasConsoleExecutionsLatestBits");
    var autoHealInvocationFailuresTask = dp.Kusto.ExecuteQuery(GetAutoHealInvocationFailures(cxt), cxt.Resource.Stamp.InternalName, null, "GetAutoHealInvocationFailures");
    var daasThrottlesTask = dp.Kusto.ExecuteQuery(GetDaasThrottles(cxt), cxt.Resource.Stamp.InternalName, null, "GetDaasThrottles");
    var insights = new List<Insight>();
    var responseElements = new List<object>();
    
    var subId = cxt.Resource.SubscriptionId;
    var rg = cxt.Resource.ResourceGroup;
    var name = cxt.Resource.Name;
    var slot = cxt.Resource.Slot;

    var RecycleReasons = new Dictionary<string, string>();

    RecycleReasons[DiagnosticConstants.PercentSlowRequests] = "Your application process was responding slow and more than 80% of the requests to the application was taking over 200 seconds in 5 minute interval. When this condition is met, App Services Platform proactive recyles your application process. For more information, check out <a href='https://azure.github.io/AppService/2017/08/17/Introducing-Proactive-Auto-Heal.html' target='_blank'>Proactive Auto-Heal</a> documentation.";
    RecycleReasons[DiagnosticConstants.TotalRequests] = "Your application was recycled because it hit the Total Request count limit defined in the autohealing configuration";
    RecycleReasons[DiagnosticConstants.Memory] = "Your application was recycled because it hit the Memory limit (privateBytesInKb) defined in the autohealing configuration";
    RecycleReasons[DiagnosticConstants.StatusCode] = "Your application was recycled because it hit the HTTP Status Code limits defined in the autohealing configuration";
    RecycleReasons[DiagnosticConstants.PercentMemory] = "Your application process was consuming more than 90% of the available memory. When this condition is met, App Services Platform proactive recyles your application process. For more information, check out <a href='https://azure.github.io/AppService/2017/08/17/Introducing-Proactive-Auto-Heal.html' target='_blank'>Proactive Auto-Heal</a> documentation.";
    RecycleReasons[DiagnosticConstants.SlowRequests] = "Your application was recycled because it hit the Request Duration limits defined in the autohealing configuration";

    var slotTimeRangesTask = dp.Observer.GetRuntimeSiteSlotMap(cxt.Resource.Stamp.InternalName, cxt.Resource.Name);
    var geoMasterTask = dp.GeoMaster.MakeHttpGetRequest<GeoMasterResponse>(subId, 
               rg, 
               name,
               slot,
               "config/web");
    
    bool triggered = false;
    
    var recylesForGraphTask = dp.Kusto.ExecuteQuery(GetAutoHealingEventsQueryForGraph(cxt), cxt.Resource.Stamp.Name, null, "GetAutoHealingEventsQueryForGraph");    
    var recycles = await dp.Kusto.ExecuteQuery(GetAutoHealingEventsQuery(cxt), cxt.Resource.Stamp.Name, null, "GetAutoHealingEventsQuery");    
    
    var slotTimeRanges = await slotTimeRangesTask;
    recycles = Utilities.GetSlotEvents(cxt.Resource.Slot, slotTimeRanges, recycles, "SiteName", "PreciseTimeStamp");
    recycles.Columns.Remove("SiteName");

    var recylesForGraph = await recylesForGraphTask;
    recylesForGraph= Utilities.GetSlotEvents(cxt.Resource.Slot, slotTimeRanges, recylesForGraph, "SiteName", "PreciseTimeStamp");
    recylesForGraph.Columns.Remove("SiteName");

    triggered = recycles.Rows.Count > 0;
    
    var recyclesGroupedByReason = recycles.Rows.Cast<DataRow>()
                                    .GroupBy( row=> new { Trigger = row["Trigger"].ToString() }  )
                                    .Select(x => new {
                                            x.Key.Trigger,
                                            Count = x.Sum( singleRow => int.TryParse(singleRow["Count"].ToString(), out int recycleCount) ? recycleCount : 0 )
                                        }
                                    );
    
    var totalRecyles = recyclesGroupedByReason.Sum(x => x.Count);

    var tblRecycles = new DataTable();
    tblRecycles.Columns.Add("Reason");
    tblRecycles.Columns.Add("Count");
    tblRecycles.Columns.Add("Description");

    foreach(var item in recyclesGroupedByReason)
    {
        var row = tblRecycles.NewRow();
        row["Reason"] = item.Trigger;
        row["Count"] = item.Count;
        row["Description"] = RecycleReasons[item.Trigger];
        tblRecycles.Rows.Add(row);
    }

    var reycleMarkdown = DetectorUtils.DataTableToMarkdown(tblRecycles);

    if(triggered)
    {
        Insight insight = null;
        if (totalRecyles > 10)
        {
            var dict = new Dictionary<string, string>();
            dict["Description"] = $"<markdown>Misconfigured Auto-Heal rules can cause frequent app restarts, which may be undesirable. The below table summarizes the Autoheal recycle events. {  Environment.NewLine + Environment.NewLine} {reycleMarkdown}</markdown>";
            dict["Recommendation"] = "Review your autohealing configuration and make sure your Auto-Heal configurations are not causing undesired app restarts" ;
            insight = new Insight(InsightStatus.Critical, "We found a high number of Auto-Heal recycle events in this time interval", dict, true);
        }
        else
        {
            var dict = new Dictionary<string, string>();
            dict["Description"] = $"<markdown>The below table summarizes the Autoheal recycle events.{  Environment.NewLine + Environment.NewLine} {reycleMarkdown}</markdown>";
            insight = new Insight(InsightStatus.Success, "We found Auto-Heal process recycle event(s) in this time interval", dict);
        }
        insights.Add(insight);        

        responseElements.Add(new DiagnosticData()
        {
            Table = recylesForGraph,
            RenderingProperties = new TimeSeriesRendering()
            {
                Title = "Recycle Events",
                Description = "The below graph shows the number of times process recycles happened due to AutoHealing feature of Azure App Service",
                GraphType = TimeSeriesType.BarGraph,
                GraphOptions = new
                    {
                        chart = new { height = 200 }
                    }
            }

        });
    }
    

    var daasConsoleTable = await daasConsoleTask;
    var daasConsoleLatestTable = await daasConsoleLatestTask;
    
    var autoHealInvocationFailures = await autoHealInvocationFailuresTask;
    //autoHealInvocationFailures = Utilities.GetSlotEvents(cxt.Resource.Slot, slotTimeRanges, autoHealInvocationFailures, "SiteName");
    autoHealInvocationFailures.Columns.Remove("SiteName");

    if (autoHealInvocationFailures.Rows.Count > 0)
    {
        triggered = true;
        var knownDaasConsoleMissingFailure = autoHealInvocationFailures.Rows.Cast<DataRow>().Any(row => row["Action"].ToString().ToLower().Contains("daasconsole.exe"));

        var dict = new Dictionary<string, string>();
        Insight insight = new Insight(InsightStatus.Critical, "We found that Auto-Heal tried invoking Diagnostics As a Service but encountered a failure", dict, true);

        bool fileNotFound = false;
        string errorCode = string.Empty;
        foreach(DataRow row in autoHealInvocationFailures.Rows)
        {
            if(row["ErrorCode"].ToString() == "0x80070002")
            {
                row["ErrorCode"] = "FILE_NOT_FOUND";
                if (!fileNotFound)
                {
                    fileNotFound = true;
                    errorCode = row["ErrorCode"].ToString();
                }
            }
        }
        string customAction = autoHealInvocationFailures.Rows[0]["Action"].ToString();
            
        if (knownDaasConsoleMissingFailure)
        {   
            dict["Description"] = $"<markdown>Auto-Heal tried invoking DaasConsole but encountered a failure because it failed to find the file `{customAction}`.</markdown>";

            dict["Root Cause"] = @"<markdown>
            This can happen due to two reasons :-
            1. `DaasConsole.exe` does not exist by default in the `D:\home\\data\DaaS\bin\` directory. It gets copied when you make the first request to the **Diagnostic Tools** section for your App which warms up the **Diagnostics** site extension.
            2. This app has **`WEBSITE_LOCAL_CACHE_OPTION`** enabled. Diagnostics As Service does not work today with Local Cache
            </markdown>";
            
            dict["Next Steps"] = @"<markdown>
            To resolve this issue, follow these steps:-
            1. If you have **Local Cache** enabled, disable it temporarily to enable data collection. You can disable **Local Cache** by setting **`WEBSITE_LOCAL_CACHE_OPTION`** to `Off` or removing the App setting completely.
            2. Navigate to the **Diagnostic Tools** section under **Diagnose and Solve** blade to make sure that this file gets created. Also validate the existence of this file by going to the KUDU console for your app. If DaasConsole.exe still doesn't get created, restart the site once and perform this step again.
            </markdown>";
            
            insights.Add(insight);
        }
        else
        {            
            insight.Message = "We found that Auto-Heal tried invoking a custom action but encountered a failure";
            if (fileNotFound)
            {
                dict["Description"] = $"<markdown>Auto-Heal tried invoking custom action but encountered a failure because the file `{customAction}` does not exist.</markdown>";
                dict["Next Steps"] = $"<markdown>Please ensure that the file `{customAction}` exists before configuring it as an autohealing custom action. You can verify this by going to the KUDU console for the app.</markdown>";
            }
            else
            {
                dict["Description"] = $"<markdown>Auto-Heal tried invoking custom action `{customAction}` but encountered a failure `{errorCode}`. This can happen if the executable for the action does not exist or failed to start.</markdown>";
            }                        
            dict["Additional Information"] = "Check the Auto-Heal invocation failures table for more details on the action invoked.";
            
            insights.Add(insight);
        }

        responseElements.Add(new DiagnosticData()
            {
                Table = autoHealInvocationFailures,
                RenderingProperties = new TableRendering()
                {
                    Title = "Auto-Heal Invocation Failures",
                    Description = "The below table shows the number of times Auto-Healing tried exeucting a custom action and got a failure. "
                 }
            });
        
    }

    DataTable daasConsole = null;

    if (daasConsoleTable.Rows.Count > 0)
    {
        daasConsole = daasConsoleTable;
    }
    else if (daasConsoleLatestTable.Rows.Count > 0)
    {
        daasConsole = daasConsoleLatestTable;
    }

    if (daasConsole != null && daasConsole.Rows.Count > 0)
    {
        daasConsole = Utilities.GetSlotEvents(cxt.Resource.Slot, slotTimeRanges, daasConsole, "SiteName", "TIMESTAMP");
        daasConsole.Columns.Remove("SiteName");

        if (daasConsole.Rows.Count > 0)
        {
            triggered = true;    
        
            var dict = new Dictionary<string, string>();
            dict["Description"] = $"DaasConsole.exe is launched if you have configured AutoHealing to run a Custom Action to collect additional data like memory dumps, profiler etc.";
            insights.Add(new Insight(InsightStatus.Warning, "We found DaasConsole invocations in this time interval.", dict));

            responseElements.Add(new DiagnosticData()
            {
                Table = daasConsole,
                RenderingProperties = new TimeSeriesRendering()
                {
                    Title = "Custom Actions",
                    Description = "The below graph shows the number of times the Diagnostic tool custom action was invoked via DaasConsole program",
                    GraphType = TimeSeriesType.BarGraph,
                    GraphOptions = new
                    {
                        chart = new { height = 200 }
                    }
                }
            });
        }
    }

    var daasThrottles = await daasThrottlesTask;
    daasThrottles = Utilities.GetSlotEvents(cxt.Resource.Slot, slotTimeRanges, daasThrottles, "SiteName");
    daasThrottles.Columns.Remove("SiteName");

    if (daasThrottles.Rows.Count > 0)
    {
        triggered = true;
        var legendDictionary = new Dictionary<string, string>();
        foreach(DataRow row in daasThrottles.Rows)
        {
            string longMessage = row["ExceptionMessage"].ToString();
            var shortMessage = ShortenExceptionMessage(longMessage);
            row["ExceptionMessage"] = shortMessage;

            if(!legendDictionary.ContainsKey(shortMessage))
            {
                legendDictionary.Add(shortMessage, longMessage);
            }
        }
        
        responseElements.Add(new DiagnosticData()
            {
                Table = daasThrottles,
                RenderingProperties = new TimeSeriesRendering()
                {
                    Title = "Throttled Diagnostic Sessions",
                    Description = "The below graph shows the number of times the diagnostic session was throttled and the reason for throttling. This is done as a preventive measure to ensure that too many data collections don't impact your app. These are typically a result of loosely configured Auto-Heal rules so check your Auto-Heal configuration and make sure that you are not invoking Auto-Healing custom actions aggressively.",            
                    GraphType = TimeSeriesType.BarGraph,
                    GraphOptions = new
                    {
                        chart = new { height = 200 }
                    }
                }
            });

        var dict = new Dictionary<string, string>();
        dict["Description"] = "Some of the diagnostic sessions triggered via autohealing were throttled. This is done as a preventive measure to ensure that too many data collections don't impact your app. These are typically a result of loosely configured Auto-Heal rules so check your Auto-Heal configuration and make sure that you are not invoking Auto-Healing custom actions aggressively.";
        dict["Next Steps"] = "Review Auto-Heal configuring for your app and ensure that you do not have loosely configured auto-heal rules that end up invoking diagnostics many times";
        Insight insight = new Insight(InsightStatus.Critical, "Throtlled Diagnostic Sessions detected", dict, true);
        insights.Add(insight); 
        
        var htmlLegend = DetectorUtils.DictionaryToMarkdown(legendDictionary, "Throttle", "Description");
        responseElements.Add(new MarkdownView(htmlLegend, "", false));
    }

    if (!triggered)
    {
        Insight insight = new Insight(InsightStatus.Success, "We found no Auto-Heal events in this time interval");
        insights.Add(insight); 
    }
    foreach(var i in insights.OrderBy(x => x.Status))
    {
        res.AddInsight(i);
    }

    if (cxt.IsInternalCall)
    {
        var resp = await geoMasterTask;
        if (resp.Properties["autoHealRules"] !=null && resp.Properties["autoHealEnabled"] != null)
        {
            string autoHealEnabled = resp.Properties["autoHealEnabled"].ToString();
            string autoHealRules = JsonConvert.SerializeObject(resp.Properties["autoHealRules"]);
            autoHealRules = CredentialTrapper.Process(autoHealRules);
            autoHealRules = "`" + Environment.NewLine + autoHealRules + Environment.NewLine + "`";
            autoHealRules = "AutoHealEnabled = " + autoHealEnabled + Environment.NewLine + Environment.NewLine + autoHealRules;
            responseElements.Add(new MarkdownView(autoHealRules, "AutoHealing Configuration - Current Settings", true));
        }
    }

    foreach(var r in responseElements)
    {
        if (r is DiagnosticData diagnosticData)
        {
            res.Dataset.Add(diagnosticData);
        }
        else if (r is MarkdownView markdownView)
        {
            if (markdownView.AddContainer)
            {
                res.AddMarkdownView(markdownView.Text, markdownView.Title);
            }
            else
            {
                res.AddMarkdownView(markdownView.Text, false, null, false);
            }
        }

    }
    
    return res;
}

static string ShortenExceptionMessage(string longMessage)
{
    string shortMessage = longMessage;
    if (longMessage.Contains("There is already another session for"))
    {
        shortMessage = "AnotherSessionInProgress";
    }
    else if (longMessage.Contains("To avoid impact to application and disk space"))
    {
        shortMessage = "SessionRejectedToSaveDisk";
    }
    else if (longMessage.Contains("The limit of maximum number of DaaS sessions"))
    {
        shortMessage = "MaxSessionsPerDay";
    }
    return shortMessage;
}

public class MarkdownView
{
    public string Text {get; set;}
    public string Title {get; set;}
    public bool AddContainer {get; set;}

    public MarkdownView(string _text, string _title, bool _addContainer)
    {
        Text = _text;
        Title = _title;
        AddContainer = _addContainer;
    }
}
