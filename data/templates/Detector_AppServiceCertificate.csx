private static string GetQuery(OperationContext<AppServiceCertificate> cxt)
{
    return
    $@"
        let startTime = datetime({cxt.StartTime});
        let endTime = datetime({cxt.EndTime});
        <YOUR_TABLE_NAME>
        | where Timestamp >= startTime and Timestamp <= endTime
        | <YOUR_QUERY>";
}

[AppServiceCertificateFilter]
[Definition(Id = "<YOUR_DETECTOR_ID>", Name = "", Author = "<YOUR_ALIAS>", Description = "")]
public async static Task<Response> Run(DataProviders dp, OperationContext<AppServiceCertificate> cxt, Response res)
{
    res.Dataset.Add(new DiagnosticData()
    {
        Table = await dp.Kusto.ExecuteClusterQuery(GetQuery(cxt))
    });

    return res;
}