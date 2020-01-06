using System.Collections.Generic;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;

namespace Diagnostics.ModelsAndUtils.Models
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

    public class DetectorCollectionRendering : Rendering
    {
        public IEnumerable<string> DetectorIds { get; set; }

        public string MessageIfCritical { get; set; }

        public DetectorCollectionRendering() : base(RenderingType.Detector)
        {
        }
    }

    public class TableRendering : Rendering
    {
        public IEnumerable<string> DisplayColumnNames { get; set; }

        public string GroupByColumnName { get; set; }

        public string DescriptionColumnName { get; set; }

        public string Height { get; set; }

        public dynamic TableOptions { get; set; }

        public bool AllowColumnSearch { get; set; }

        public TableRendering() : base(RenderingType.Table)
        {
            DisplayColumnNames = null;
            GroupByColumnName = null;
            DescriptionColumnName = null;
            AllowColumnSearch = false;
        }
    }

    public class TimeSeriesRendering : Rendering
    {
        public int DefaultValue { get; set; }

        public TimeSeriesType GraphType { get; set; }

        public dynamic GraphOptions { get; set; }

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
        public int DefaultValue { get; set; }

        public TimeSeriesType GraphType { get; set; }

        public dynamic GraphOptions { get; set; }

        public string TimestampColumnName { get; set; }

        public string RoleInstanceColumnName { get; set; }

        public string CounterColumnName { get; set; }

        public string ValueColumnName { get; set; }

        public IEnumerable<string> InstanceFilter { get; set; }

        public IEnumerable<string> CounterNameFilter { get; set; }

        public string SelectedInstance { get; set; }

        public TimeSeriesPerInstanceRendering() : base(RenderingType.TimeSeriesPerInstance)
        {
            DefaultValue = 0;
            GraphType = TimeSeriesType.LineGraph;
        }
    }

    public class DynamicInsightRendering : Rendering
    {
        public InsightStatus Status { get; set; }

        public Rendering InnerRendering { get; set; }

        public bool Expanded { get; set; }

        public DynamicInsightRendering() : base(RenderingType.DynamicInsight)
        {
            Expanded = true;
        }
    }

    public class MarkdownRendering : Rendering
    {
        public bool EnableEmailButtons { get; set; }

        public bool IsContainerNeeded { get; set; }

        public MarkdownRendering() : base(RenderingType.Markdown)
        {
            EnableEmailButtons = false;
            IsContainerNeeded = true;
        }
    }

    public enum RenderingType
    {
        /// <summary>
        /// No Graph
        /// </summary>
        NoGraph = 0,

        /// <summary>
        /// Data rendered as Table
        /// </summary>
        Table,

        /// <summary>
        /// Data rendered as Time Series. <seealso cref="TimeSeriesType"/>
        /// </summary>
        TimeSeries,

        /// <summary>
        /// Data rendered as Time Series for every instance.
        /// </summary>
        TimeSeriesPerInstance,

        /// <summary>
        /// Data rendered as Pie Chart
        /// </summary>
        PieChart,

        /// <summary>
        /// Data rendered as Summary Points with Title and Value.
        /// </summary>
        DataSummary,

        /// <summary>
        /// Data rendered as Email.
        /// </summary>
        Email,

        /// <summary>
        /// Data rendered as a basic insight
        /// </summary>
        Insights,

        /// <summary>
        /// Data rendered as a dynamic insight. You can put any other rendering type inside this one.
        /// </summary>
        DynamicInsight,

        /// <summary>
        /// Data rendered as a markdown document. Just a single string.
        /// </summary>
        Markdown,

        /// <summary>
        /// This will pass the definition of a detector that will be rendered in the view
        /// </summary>
        Detector,

        /// <summary>
        /// Data rendered as (key, value) pair where key goes in dropdown and value goes in body.
        /// </summary>
        DropDown,

        /// <summary>
        /// Data rendered as Tile with a title, some text and a link to another detector or a diagnostic tool
        /// </summary>
        Card,

        /// <summary>
        /// Solution actions that can be added to Insights
        /// </summary>
        Solution,

        /// <summary>
        /// Data rendered as Guage with a label, some description text
        /// </summary>
        Guage,

        /// <summary>
        /// Data rendered as form with inputs fields and buttons.
        /// </summary>
        Form,

        /// <summary>
        /// Data rendered as Change Sets.
        /// </summary>
        ChangeSets,

        /// <summary>
        /// Data rendered for onboarding view
        /// </summary>
        ChangeAnalysisOnboarding,

        /// <summary>
        /// Data rendered as Changes
        /// </summary>
        ChangesView,

        /// <summary>
        /// Data rendered for appliation insight
        /// </summary>
        AppInsight,

        /// Dependent resources of web app rendered as graph.
        /// </summary>
        DependencyGraph,

        /// <summary>
        /// Data Rendered as downtimes in the Analysis View
        /// </summary>
        DownTime,

        /// <summary>
        /// Data Rendered as only summarized message
        /// </summary>
        SummaryCard,

        /// <summary>
        /// Render search component inside detector
        /// </summary>
        SearchComponent,

        ///<summary>
        /// Data Rendered as AppInsights enablement in detector
        ///</summary>
        AppInsightEnablement

    }

    /// <summary>
    /// Defines Type of Time Series
    /// </summary>
    public enum TimeSeriesType
    {
        LineGraph = 0,
        BarGraph,
        StackedAreaGraph,
        StackedBarGraph
    }
}
