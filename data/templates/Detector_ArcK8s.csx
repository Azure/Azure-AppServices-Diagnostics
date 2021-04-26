private static string GetQuery(OperationContext<AzureCosmosDB> cxt)
{
    return
    $@"
        let startTime = datetime({cxt.StartTime});
        let endTime = datetime({cxt.EndTime});
        cluster('CdbSupport').database('Support').<YOUR_TABLE_NAME>
        | where Timestamp >= startTime and Timestamp <= endTime
        | <YOUR_QUERY>";
}

[ArmResourceFilter(provider: "Microsoft.Kubernetes", resourceTypeName: "connectedClusters")]
[Definition(Id = "<YOUR_DETECTOR_ID>", Name = "", Author = "<YOUR_ALIAS>", Description = "")]
public async static Task<Response> Run(DataProviders dp, OperationContext<ArmResource> cxt, Response res)
{
    res.Dataset.Add(new DiagnosticData()
    {
        Table = await dp.Kusto.ExecuteClusterQuery(GetQuery(cxt), null, "GetQuery"),
        RenderingProperties = new Rendering(RenderingType.Table){
            Title = "Sample Table", 
            Description = "Some description here"
        }
    });

    return res;
}
