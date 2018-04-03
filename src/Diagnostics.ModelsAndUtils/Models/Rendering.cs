using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils
{
    public class Rendering
    {
        public RenderingType Type { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public Rendering()
        {
            Type = RenderingType.TimeSeries;
        }

        public Rendering(RenderingType type)
        {
            Type = type;
        }
    }

    public class TableRendering : Rendering
    {
        public IEnumerable<string> DisplayColumnNames { get; set; }

        public string GroupByColumnName { get; set; }

        public TableRendering() : base(RenderingType.Table)
        {
            DisplayColumnNames = null;
            GroupByColumnName = null;
        }
    }

    public class TimeSeriesRendering : Rendering
    {
        public int DefaultValue { get; set; }

        public TimeSeriesType GraphType { get; set; }

        public string TimestampColumnName { get; set; }

        public string RoleInstanceColumnName { get; set; }

        public IEnumerable<string> SeriesColumns { get; set; }

        public TimeSeriesRendering()
        {
            DefaultValue = 0;
            GraphType = TimeSeriesType.LineGraph;
        }
    }

    public class TimeSeriesPerInstanceRendering : Rendering
    {
        public TimeSeriesType GraphType { get; set; }

        public string TimestampColumnName { get; set; }

        public string RoleInstanceColumnName { get; set; }

        public string CounterColumnName { get; set; }

        public string ValueColumnName { get; set; }

        public IEnumerable<string> InstanceFilter { get; set; }

        public IEnumerable<string> CounterNameFilter { get; set; }

        public string SelectedInstance { get; set; }
    }

    public enum RenderingType
    {
        NoGraph = 0,
        Table,
        TimeSeries,
        TimeSeriesPerInstance,
        PieChart
    }

    public enum TimeSeriesType
    {
        LineGraph = 0,
        BarGraph,
        StackedAreaGraph,
        StackedBarGraph
    }
}
