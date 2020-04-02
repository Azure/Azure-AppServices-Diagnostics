private static string _latestKuduDeployQuery = @"
Kudu
| where PreciseTimeStamp >= ago(30d) and EventPrimaryStampName =~ ""{eventPrimaryStampName}""
| where TaskName =~ ""ProjectDeployed"" and (siteName =~ ""{siteName}"" or siteName startswith ""{siteName}__"")
| summarize LatestDeployment = max(PreciseTimeStamp), TotalDeployments30d = count()
";

private static string _latestMsDeployQuery = @"
MSDeploy
| where PreciseTimeStamp >= ago(30d) and EventPrimaryStampName =~ ""{eventPrimaryStampName}""
| where (SiteName =~ ""~1{siteName}"" or SiteName startswith ""~1{siteName}__"")
| where (Message contains ""PassId: 1"" or Message contains ""Package deployed successfully"" )
| summarize LatestDeployment = max(PreciseTimeStamp), TotalDeployments30d = count()
";

[Definition(Id = "backupcheckerdetector", Name = "Check Backup", Description = "Check for optimal use of backup/restore feature")]
public async static Task<Response> Run(DataProviders dp, OperationContext cxt, Response res)
{
    string observationStatement = null;
    string resolution = null;

    var siteDataTask = dp.Observer.GetSite(cxt.Resource.Stamp, cxt.Resource.SiteName);
    var latestKuduDeploymentTask = dp.Kusto.ExecuteQuery(_latestKuduDeployQuery.Replace("{siteName}", cxt.Resource.SiteName).Replace("{eventPrimaryStampName}", cxt.Resource.Stamp.InternalName), cxt.Resource.Stamp, operationName: "GetLatestDeployment");
    var latestMsDeploymentTask = dp.Kusto.ExecuteQuery(_latestMsDeployQuery.Replace("{siteName}", cxt.Resource.SiteName).Replace("{eventPrimaryStampName}", cxt.Resource.Stamp.InternalName), cxt.Resource.Stamp, operationName: "GetLatestDeployment");

    await Task.WhenAll(new Task[] { siteDataTask, latestKuduDeploymentTask, latestMsDeploymentTask });

    var siteData = await siteDataTask;

    try
    {
        if (siteData[0].sku == "Premium" || siteData[0].sku == "Standard")
        {
            if (siteData[0].backups.Count == 0)
            {
                Console.WriteLine(siteData[0].backups.Count);
                observationStatement = "Your website does not have any backups. It may not have backup/restore feature enabled.";
                resolution = $"Enable backup/restore feature for your website. It comes free with {siteData[0].sku} sku websites";
            }
            else
            {
                var latestBackupTimeStamp = siteData[0].backups[siteData[0].backups.Count - 1].Value<DateTime>("finished_time_stamp");
                var latestKuduDeploymentTimeStamp = (await latestKuduDeploymentTask).Rows[0][0];
                var latestMsDeploymentTimeStamp = (await latestMsDeploymentTask).Rows[0][0];

                DateTime latestDeploymentTimeStamp = default(DateTime);
                int outstandingDays = 0;

                if (!string.IsNullOrWhiteSpace(latestKuduDeploymentTimeStamp))
                {
                    latestDeploymentTimeStamp = DateTime.Parse(latestKuduDeploymentTimeStamp);
                }

                if (!string.IsNullOrWhiteSpace(latestMsDeploymentTimeStamp))
                {
                    var tmpTimeStamp = DateTime.Parse(latestMsDeploymentTimeStamp);
                    latestDeploymentTimeStamp = new DateTime(Math.Max(latestDeploymentTimeStamp.Ticks, tmpTimeStamp.Ticks));
                }

                if (latestDeploymentTimeStamp != default(DateTime))
                {
                    outstandingDays = (int)Math.Abs((latestDeploymentTimeStamp - latestBackupTimeStamp).TotalDays);
                }

                if (outstandingDays >= 1)
                {
                    observationStatement = $"Your last backup is {outstandingDays} days old.";
                    resolution = "Create a new backup or enable the automatic backup feature";
                }
                else
                {
                    observationStatement = $"Your backups are only less than a day old. This is good but take a backup at your earliest convenience to protect your site.";
                }
            }
        }
        else
        {
            observationStatement = "Your website does not have backup/restore feature enabled. This is only offered or standard and premium sku websites. To learn more about this feature click here https://docs.microsoft.com/en-us/azure/app-service/web-sites-backup";
        }

        var dataSummaries = new List<DataSummary>(){
        new DataSummary("Observation", observationStatement)
    };

        if (!string.IsNullOrWhiteSpace(resolution))
        {
            dataSummaries.Add(new DataSummary("Solution", resolution));
        }

        res.AddDataSummary(dataSummaries);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        throw;
    }

    return res;
}
