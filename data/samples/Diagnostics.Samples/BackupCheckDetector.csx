private static string _latestKuduDeployQuery = @"
Kudu
| where PreciseTimeStamp >= ago(30d) and Tenant =~ ""{tenantId}""
| where TaskName =~ ""ProjectDeployed"" and (siteName =~ ""{siteName}"" or siteName startswith ""{siteName}__"")
| summarize max(PreciseTimeStamp), TotalDeployments30d = count()
";

private static string _latestMsDeployQuery = @"
MSDeploy
| where PreciseTimeStamp >= ago(30d) and Tenant =~ ""{tenantId}""
| where (SiteName =~ ""~1{siteName}"" or SiteName startswith ""~1{siteName}__"")
| where (Message contains ""PassId: 1"" or Message contains ""Package deployed successfully"" )
| summarize max(PreciseTimeStamp), TotalDeployments30d = count()
";

[Definition(Id = "backupcheckerdetector", Name = "Check Backup", Description = "Check for optimal use of backup/restore feature")]
public async static Task<Response> Run(DataProviders dp, OperationContext cxt, Response res)
{
    var siteDataTask = dp.Observer.GetSite(cxt.Resource.Stamp, cxt.Resource.SiteName);
    var latestKuduDeploymentTask = dp.Kusto.ExecuteQuery(_latestKuduDeployQuery.Replace("{siteName}", cxt.Resource.SiteName).Replace("{tenantId}", (cxt.Resource.TenantIdList as List<string>)[0]), cxt.Resource.Stamp);
    var latestMsDeploymentTask = dp.Kusto.ExecuteQuery(_latestMsDeployQuery.Replace("{siteName}", cxt.Resource.SiteName).Replace("{tenantId}", (cxt.Resource.TenantIdList as List<string>)[0]), cxt.Resource.Stamp);

    await Task.WhenAll(new Task[] { siteDataTask, latestKuduDeploymentTask, latestMsDeploymentTask });

    var siteData = await siteDataTask;

    if (siteData[0].sku == "Premium" || siteData[0].sku == "Standard")
    {
        Console.WriteLine("hello it works");
    }
    return res;
}