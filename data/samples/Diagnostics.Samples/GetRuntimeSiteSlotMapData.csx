using System;
using System.Collections.Generic;
using System.Linq;

[Definition(Id = "GetRuntimeSiteSlotMapData", Name = "Runtime SiteName History", Description = "This represents the history of runtime site names.")]
public async static Task<Response> Run(DataProviders dp, OperationContext cxt, Response res)
{
    var runtimeSiteSlotMap = await dp.Observer.GetRuntimeSiteSlotMap(cxt.Resource.Stamp, cxt.Resource.SiteName);
    var diagnosticData = new DiagnosticData();
    diagnosticData.RenderingProperties = new Rendering(RenderingType.Table);
    diagnosticData.Table.TableName = "n/a";
    var list = new List<DataTableResponseColumn>();
    list.Add(new DataTableResponseColumn() { ColumnName = "SlotName", DataType = "string", ColumnType = "string" });
    list.Add(new DataTableResponseColumn() { ColumnName = "RuntimeSiteName", DataType = "string", ColumnType = "string" });
    list.Add(new DataTableResponseColumn() { ColumnName = "StartTime", DataType = "datetime", ColumnType = "datetime" });
    list.Add(new DataTableResponseColumn() { ColumnName = "EndTime", DataType = "datetime", ColumnType = "datetime" });
    diagnosticData.Table.Columns = list;

    diagnosticData.Table.Rows = new string[10][];


    for (int i = 0; i < runtimeSiteSlotMap.Keys.Count; i++)
    {
        var slotName = runtimeSiteSlotMap.Keys.ElementAt(i);
        foreach (var slotInfo in runtimeSiteSlotMap[slotName])
        {
            diagnosticData.Table.Rows[i] = new string[4] { slotName, slotInfo.RuntimeSitename, slotInfo.StartTime.ToString(), slotInfo.EndTime.ToString() };
        }
    }
    res.Dataset.Add(diagnosticData);

    return res;
}
