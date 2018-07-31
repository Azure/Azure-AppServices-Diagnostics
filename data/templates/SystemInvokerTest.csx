private static string GetQuery(OperationContext<IResource> cxt)
{
    return
    $@"<YOUR_TABLE_NAME>
        | where {Utilities.TimeAndTenantFilterQuery(cxt.StartTime, cxt.EndTime, cxt.Resource)}
        | <YOUR_QUERY>";
}

[SystemFilter]
[Definition(Id = "<YOUR_DETECTOR_ID>", Name = "<YOUR_DETECTOR_NAME>", Author = "<YOUR_ALIAS>", Description = "")]
public async static Task<Response> Run(DataProviders dp, OperationContext<IResource> cxt, Response res)
{
    res.Dataset.Add(new DiagnosticData()
    {
		Table = await dp.Kusto.ExecuteClusterQuery(GetQuery(cxt))
    });

    return res;
}