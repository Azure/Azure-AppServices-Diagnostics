using System;
using System.Collections.Generic;
using System.Linq;

[AppFilter]
[Definition(Id = "GetRuntimeSiteSlotMapData", Name = "Runtime SiteName History", Description = "This represents the history of runtime site names.")]
public async static Task<Response> Run(DataProviders dp, OperationContext<App> cxt, Response res)
{
    var runtimeSiteSlotMap = await dp.Observer.GetRuntimeSiteSlotMap(cxt.Resource.Stamp.Name, cxt.Resource.Name);
    var diagnosticData = new DiagnosticData();
    diagnosticData.RenderingProperties = new Rendering(RenderingType.Table);

    var table = new DataTable();

    table.Columns.AddRange(new DataColumn[]
    {
        new DataColumn("SlotName", typeof(string)),
        new DataColumn("RuntimeSiteName", typeof(string)),
        new DataColumn("StartTime", typeof(DateTime)),
        new DataColumn("EndTime", typeof(DateTime))
    });

    for (int i = 0; i < runtimeSiteSlotMap.Keys.Count; i++)
    {
        var slotName = runtimeSiteSlotMap.Keys.ElementAt(i);
        foreach (var slotInfo in runtimeSiteSlotMap[slotName])
        {
            table.Rows.Add(new string[4] { slotName, slotInfo.RuntimeSitename, slotInfo.StartTime.ToString(), slotInfo.EndTime.ToString() });
        }
    }

    diagnosticData.Table = table;

    res.Dataset.Add(diagnosticData);

    return res;
}
