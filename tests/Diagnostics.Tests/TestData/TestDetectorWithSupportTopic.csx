private static string GetQuery(OperationContext<App> cxt)
{
    return
    $@"<YOUR_TABLE_NAME>
        | where {Utilities.TimeAndTenantFilterQuery(cxt.StartTime, cxt.EndTime, cxt.Resource)}
        | <YOUR_QUERY>";
}

[SupportTopic(Id = "<SUPPORT_TOPIC_ID_1>", PesId = "<PES_ID_1>", SapProductId = "<SAP_PRODUCT_ID_1>", SapSupportTopicId = "<SAP_SUPPORT_TOPIC_ID_1>")]
[SupportTopic(Id = "<SUPPORT_TOPIC_ID_2>", PesId = "<PES_ID_2>", SapProductId = "<SAP_PRODUCT_ID_2>", SapSupportTopicId = "<SAP_SUPPORT_TOPIC_ID_2>")]
[SupportTopic(Id = "<SUPPORT_TOPIC_ID_3>", PesId = "<PES_ID_3>", SapProductId = "<SAP_PRODUCT_ID_3>", SapSupportTopicId = "<SAP_SUPPORT_TOPIC_ID_3>")]
[AppFilter(AppType = AppType.WebApp, PlatformType = PlatformType.Windows, StackType = StackType.All, InternalOnly = "<INTERNAL_FLAG>")]
[Definition(Id = "<YOUR_DETECTOR_ID>", Name = "<YOUR_DETECTOR_NAME>", Author = "<YOUR_ALIAS>", Description = "")]
public async static Task<Response> Run(DataProviders dp, OperationContext<App> cxt, Response res)
{
    res.Dataset.Add(new DiagnosticData()
    {
        Table = await dp.Kusto.ExecuteQuery(GetQuery(cxt), cxt.Resource.Stamp.Name)
    });

    return res;
}
